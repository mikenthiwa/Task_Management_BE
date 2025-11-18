using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Identity;

public class ApplicationUserManager(
    IUserStore<ApplicationUser> store,
    IOptions<IdentityOptions> optionsAccessor,
    IPasswordHasher<ApplicationUser> passwordHasher,
    IEnumerable<IUserValidator<ApplicationUser>> userValidators,
    IEnumerable<IPasswordValidator<ApplicationUser>> passwordValidators,
    ILookupNormalizer keyNormalizer,
    IdentityErrorDescriber errors,
    IServiceProvider services,
    ILogger<UserManager<ApplicationUser>> logger,
    ApplicationDbContext dbContext
    )  
    : UserManager<ApplicationUser>(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
{

    public override async Task<IdentityResult> CreateAsync(ApplicationUser user)
    {
        var result = await base.CreateAsync(user);
        if (!result.Succeeded) throw new Exception("Failed to create user in Identity store.");
        return await MirrorToDomainAsync(user, result);
    }
    

    private async Task<IdentityResult> MirrorToDomainAsync(ApplicationUser user, IdentityResult result)
    {
        if (string.IsNullOrEmpty(user.Id) || string.IsNullOrEmpty(user.UserName) || string.IsNullOrEmpty(user.Email)) return result;

        var exist = await dbContext.DomainUsers.AnyAsync(u => u.Id == user.Id);
        if(exist) return result;
        dbContext.DomainUsers.Add(new DomainUser(user.Id, user.UserName, user.Email));
        try
        {
            await dbContext.SaveChangesAsync();
            return result;
        }
        catch (Exception ex)
        {
            await base.DeleteAsync(user);
            throw new Exception(ex.Message);
        }
    }
}

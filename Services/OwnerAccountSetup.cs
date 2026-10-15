using System;
using System.Linq;
using Armelle.Data;
using Armelle.Models;
using Microsoft.Extensions.Configuration;

namespace Armelle.Services;

public static class OwnerAccountSetup
{
    public static void Apply(ClinicDb db, IConfiguration configuration)
    {
        var owners = new[]
        {
            (Username: "angeline", Key: "ARMELLE_OWNER_ANGELINE_PASSWORD"),
            (Username: "ricky", Key: "ARMELLE_OWNER_RICKY_PASSWORD"),
            (Username: "mica", Key: "ARMELLE_OWNER_MICA_PASSWORD")
        };

        // Owner passwords are only applied to existing accounts during
        // an explicitly requested one-time reset.
        bool reset = string.Equals(configuration["ARMELLE_RESET_OWNER_PASSWORDS"],
            "true", StringComparison.OrdinalIgnoreCase);

        foreach (var owner in owners)
        {
            var user = db.AppUsers.FirstOrDefault(x => x.Username == owner.Username);
            var password = configuration[owner.Key];
            bool validPassword = !string.IsNullOrWhiteSpace(password) && password.Length >= 12;

            if (user == null)
            {
                if (!validPassword)
                {
                    Console.Error.WriteLine($"[OWNER SETUP] Missing valid password for {owner.Username}; account not created.");
                    continue;
                }
                db.AppUsers.Add(new AppUser
                {
                    Username = owner.Username,
                    Role = "Owner",
                    Password = PasswordSecurity.Hash(password!)
                });
                db.SaveChanges();
                Console.WriteLine($"[OWNER SETUP] Created Owner '{owner.Username}'.");
                continue;
            }

            if (user.Role == "Owner")
            {
                // Keep the existing password; OwnerPasswordReset handles opt-in reset.
                continue;
            }

            // This clinic's designated 'mica' account is confirmed by the
            // project owner as the Owner account, not a Patient account.
            // Only promote it when an explicit one-time reset is requested
            // and a new strong password is provided. Do not change other users.
            if (owner.Username == "mica" && reset && validPassword)
            {
                user.Role = "Owner";
                user.Password = PasswordSecurity.Hash(password!);
                db.SaveChanges();
                Console.WriteLine("[OWNER SETUP] Converted confirmed 'mica' account to Owner and set a new password.");
            }
            else
            {
                Console.Error.WriteLine($"[OWNER SETUP] '{owner.Username}' has role '{user.Role}'. No change made. For confirmed Mica account, set ARMELLE_RESET_OWNER_PASSWORDS=true and a strong ARMELLE_OWNER_MICA_PASSWORD once.");
            }
        }
    }
}

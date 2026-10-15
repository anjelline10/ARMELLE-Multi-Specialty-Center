using System;
using System.Linq;
using Armelle.Data;
using Microsoft.Extensions.Configuration;

namespace Armelle.Services;

public static class OwnerPasswordReset
{
    // Explicit opt-in only. Do not reset Owner passwords on normal startups.
    public static void Apply(ClinicDb db, IConfiguration configuration)
    {
        if (!string.Equals(configuration["ARMELLE_RESET_OWNER_PASSWORDS"], "true", StringComparison.OrdinalIgnoreCase))
            return;

        var owners = new[]
        {
            (Username: "angeline", Key: "ARMELLE_OWNER_ANGELINE_PASSWORD"),
            (Username: "ricky", Key: "ARMELLE_OWNER_RICKY_PASSWORD"),
            (Username: "mica", Key: "ARMELLE_OWNER_MICA_PASSWORD")
        };

        foreach (var owner in owners)
        {
            var user = db.AppUsers.FirstOrDefault(x => x.Username == owner.Username && x.Role == "Owner");
            if (user == null) continue;

            var password = configuration[owner.Key];
            if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
            {
                Console.Error.WriteLine($"[ARMELLE OWNER RESET] Skipped '{owner.Username}': {owner.Key} is missing or shorter than 12 characters.");
                continue;
            }
            user.Password = PasswordSecurity.Hash(password);
            db.SaveChanges();
            Console.WriteLine($"[ARMELLE OWNER RESET] Password updated for '{owner.Username}'.");
        }
        Console.WriteLine("[ARMELLE OWNER RESET] IMPORTANT: unset ARMELLE_RESET_OWNER_PASSWORDS after this one-time reset.");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using BaobabRidgeWildlifeCentre.Models;
using System.Text.RegularExpressions;

namespace BaobabRidgeWildlifeCentre.BusinessLogic
{
    public static class AnimalManager
    {
        public static void Classify(Animal animal)
        {
            if (animal.RecoveryScore <= 19)
            {
                animal.Status = "Critical";
                animal.HousingUnit = "Intensive Care Unit";
            }
            else if (animal.RecoveryScore <= 39)
            {
                animal.Status = "Serious";
                animal.HousingUnit = "High-Dependency Ward";
            }
            else if (animal.RecoveryScore <= 59)
            {
                animal.Status = "Stable";
                animal.HousingUnit = "Recovery Ward";
            }
            else if (animal.RecoveryScore <= 79)
            {
                animal.Status = "Recovering";
                animal.HousingUnit = "Outdoor Enclosure";
            }
            else
            {
                animal.Status = "Release-Ready";
                animal.HousingUnit = "Pre-Release Camp";
            }
        }

        
        public static string ValidateAll(string id, string name, string species, string ageText, string scoreText)
        {
            // 1. Check Animal ID
            if (string.IsNullOrWhiteSpace(id))
                return "Animal ID is required.";
            if (!Regex.IsMatch(id.Trim().ToUpper(), @"^WR-\d{4}$"))
                return "Animal ID must be WR- followed by exactly 4 digits (e.g. WR-0007).";

            // 2. Check Name
            if (string.IsNullOrWhiteSpace(name))
                return "Name is required.";
            if (name.Trim().Length > 30 || name.Contains("|"))
                return "Name must be 1-30 characters and cannot contain '|'.";

            // 3. Check Species
            if (string.IsNullOrWhiteSpace(species))
                return "Species is required.";
            if (species.Trim().Length > 30 || species.Contains("|"))
                return "Species must be 1-30 characters and cannot contain '|'.";

            // 4. Check Age
            if (!int.TryParse(ageText.Trim(), out int age) || age < 0 || age > 100)
                return "Age must be a whole number between 0 and 100.";

            // 5. Check Recovery Score
            if (!int.TryParse(scoreText.Trim(), out int score) || score < 0 || score > 100)
                return "Recovery Score must be a whole number between 0 and 100.";

            // Everything passed!
            return null;
        }
    }
}

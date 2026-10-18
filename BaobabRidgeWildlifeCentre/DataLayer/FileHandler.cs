using BaobabRidgeWildlifeCentre.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaobabRidgeWildlifeCentre.DataLayer
{
    public static class FileHandler
    {
        public static string AnimalsFile => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "animals.txt");
        public static string SummaryFile => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "summary.txt");

        /// <summary>
        /// Loads animals from the text file.
        /// </summary>
        public static (List<Animal> animals, int skipped, string error) LoadAnimals()
        {
            var list = new List<Animal>();
            int skipped = 0;

            try
            {
                if (!File.Exists(AnimalsFile))
                {
                    File.Create(AnimalsFile).Close();
                    return (list, 0, null);
                }

                string[] lines = File.ReadAllLines(AnimalsFile);
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) { skipped++; continue; }

                    string[] parts = line.Split('|');
                    // A valid line must have exactly 7 parts
                    if (parts.Length != 7) { skipped++; continue; }

                    // Quick check for Age and Score
                    if (!int.TryParse(parts[3], out int age) || !int.TryParse(parts[4], out int score))
                    {
                        skipped++; continue;
                    }

                    list.Add(new Animal
                    {
                        AnimalId = parts[0].Trim().ToUpper(),
                        Name = parts[1].Trim(),
                        Species = parts[2].Trim(),
                        Age = age,
                        RecoveryScore = score,
                        Status = parts[5].Trim(),
                        HousingUnit = parts[6].Trim()
                    });
                }
            }
            catch (UnauthorizedAccessException) { return (list, skipped, "Permission denied reading animals.txt."); }
            catch (IOException ex) { return (list, skipped, "File error: " + ex.Message); }

            return (list, skipped, null);
        }

        /// <summary>
        /// Overwrites animals.txt with the current list.
        /// </summary>
        public static string SaveAll(List<Animal> animals)
        {
            try
            {
                File.WriteAllLines(AnimalsFile, animals.Select(a => a.ToString()));
                return null;
            }
            catch (UnauthorizedAccessException) { return "Permission denied saving animals.txt."; }
            catch (IOException ex) { return "File error: " + ex.Message; }
        }

        /// <summary>
        /// Appends one animal to the end of animals.txt.
        /// </summary>
        public static string Append(Animal animal)
        {
            try
            {
                File.AppendAllText(AnimalsFile, animal.ToString() + Environment.NewLine);
                return null;
            }
            catch (UnauthorizedAccessException) { return "Permission denied writing to animals.txt."; }
            catch (IOException ex) { return "File error: " + ex.Message; }
        }

        /// <summary>
        /// Generates summary.txt exactly as the rubric requires.
        /// </summary>
        public static string WriteSummary(List<Animal> animals)
        {
            try
            {
                var ci = CultureInfo.InvariantCulture;
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm", ci);

                string total = animals.Count.ToString(ci);
                string avgAge = animals.Count == 0 ? "N/A" : animals.Average(a => a.Age).ToString("0.00", ci);
                string avgScore = animals.Count == 0 ? "N/A" : animals.Average(a => a.RecoveryScore).ToString("0.00", ci);

                string[] statuses = { "Critical", "Serious", "Stable", "Recovering", "Release-Ready" };

                var sb = new System.Text.StringBuilder();
                sb.AppendLine("BAOBAB RIDGE WILDLIFE REHABILITATION CENTRE");
                sb.AppendLine("ANIMAL SUMMARY REPORT");
                sb.AppendLine($"Generated: {timestamp}");
                sb.AppendLine();
                sb.AppendLine($"Total animals: {total}");
                sb.AppendLine($"Average age (years): {avgAge}");
                sb.AppendLine($"Average recovery score: {avgScore}");
                sb.AppendLine();
                sb.AppendLine("Animals per status:");

                foreach (string s in statuses)
                    sb.AppendLine($"{s}: {animals.Count(a => a.Status == s)}");

                File.WriteAllText(SummaryFile, sb.ToString());
                return null;
            }
            catch (UnauthorizedAccessException) { return "Permission denied writing summary.txt."; }
            catch (IOException ex) { return "File error: " + ex.Message; }
        }
    }
}

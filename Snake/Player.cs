using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Schema;

namespace Snake
{
    public class Player
    {
        private string _filepath = "highscores.json";
        public string Name { get; set; }
        public int Score { get; set; }
        public static Dictionary<string, int> Highscores { get; private set; } = new Dictionary<string, int>();

        public Player(string name, int score)
        {
            Name = name;
            Score = score;
            LoadHighscores();
        }

        public void AddOrUpdateScore(string playerName, int score)
        {
            if (Highscores.ContainsKey(playerName))
            {
                if (score > Highscores[playerName])
                    Highscores[playerName] = score;
            }
            else
            {
                Highscores[playerName] = score;
            }
            SaveHighscores();
        }

        public Dictionary<string, int> GetTopScores(int topN = 5)
        {
            return Highscores
                .OrderByDescending(kv => kv.Value)
                .Take(topN)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        private void SaveHighscores()
        {
            try
            {
                string json = JsonSerializer.Serialize(Highscores, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filepath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save highscores: {ex.Message}");
            }
        }

        private void LoadHighscores()
        {
            try
            {
                if (File.Exists(_filepath))
                {
                    string json = File.ReadAllText(_filepath);
                    Highscores = JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load highscores: {ex.Message}");
                Highscores = new Dictionary<string, int>();
            }
        }
    }
}
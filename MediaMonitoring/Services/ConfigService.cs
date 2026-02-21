using MediaMonitoring.Data;
using MediaMonitoring.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaMonitoring.Services
{
    /// <summary>
    /// Service untuk mengelola konfigurasi aplikasi secara dinamis.
    /// Memungkinkan pengguna mengubah API Key, Connection String, dll dari UI.
    /// </summary>
    public class ConfigService
    {
        private readonly MediaMonitoringContext _context;
        private static Dictionary<string, string>? _cache;

        public ConfigService(MediaMonitoringContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Mengambil nilai konfigurasi berdasarkan kunci.
        /// </summary>
        public async Task<string> GetValueAsync(string key, string defaultValue = "")
        {
            if (_cache == null) await LoadCacheAsync();

            if (_cache!.TryGetValue(key, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        /// <summary>
        /// Menyimpan atau memperbarui nilai konfigurasi.
        /// </summary>
        public async Task SetValueAsync(string key, string value, string description = "", bool isSensitive = false)
        {
            var config = await _context.SystemConfigurations
                .FirstOrDefaultAsync(c => c.ConfigKey == key);

            if (config == null)
            {
                config = new SystemConfiguration
                {
                    ConfigKey = key,
                    ConfigValue = value,
                    Description = description,
                    IsSensitive = isSensitive,
                    LastUpdated = DateTime.UtcNow
                };
                _context.SystemConfigurations.Add(config);
            }
            else
            {
                config.ConfigValue = value;
                config.Description = description;
                config.IsSensitive = isSensitive;
                config.LastUpdated = DateTime.UtcNow;
                _context.SystemConfigurations.Update(config);
            }

            await _context.SaveChangesAsync();
            
            // Refresh cache
            if (_cache != null)
            {
                _cache[key] = value;
            }
        }

        /// <summary>
        /// Mendapatkan semua konfigurasi untuk ditampilkan di halaman settings.
        /// </summary>
        public async Task<List<SystemConfiguration>> GetAllConfigurationsAsync()
        {
            return await _context.SystemConfigurations
                .OrderBy(c => c.ConfigKey)
                .ToListAsync();
        }

        /// <summary>
        /// Memuat cache konfigurasi ke memori.
        /// </summary>
        private async Task LoadCacheAsync()
        {
            var configs = await _context.SystemConfigurations.ToListAsync();
            _cache = configs.ToDictionary(c => c.ConfigKey, c => c.ConfigValue);
        }

        /// <summary>
        /// Inisialisasi konfigurasi default jika database masih kosong.
        /// </summary>
        public async Task InitializeDefaultsAsync()
        {
            var count = await _context.SystemConfigurations.CountAsync();
            if (count == 0)
            {
                var defaults = new List<SystemConfiguration>
                {
                    new SystemConfiguration { ConfigKey = "TwitterApiKey", ConfigValue = "", Description = "API Key Twitter/X", IsSensitive = true },
                    new SystemConfiguration { ConfigKey = "FacebookAppId", ConfigValue = "", Description = "Facebook App ID", IsSensitive = true },
                    new SystemConfiguration { ConfigKey = "CrawlIntervalMinutes", ConfigValue = "5", Description = "Interval crawling dalam menit" },
                    new SystemConfiguration { ConfigKey = "MaxPostsPerCrawl", ConfigValue = "50", Description = "Maksimal postingan per sesi crawling" },
                    new SystemConfiguration { ConfigKey = "EnableDarkWebMonitor", ConfigValue = "false", Description = "Aktifkan monitoring Dark Web (Simulasi)" },
                    new SystemConfiguration { ConfigKey = "AdminEmail", ConfigValue = "admin@localhost.com", Description = "Email admin untuk notifikasi" }
                };

                _context.SystemConfigurations.AddRange(defaults);
                await _context.SaveChangesAsync();
                
                // Reset cache
                _cache = null; 
            }
        }
    }
}
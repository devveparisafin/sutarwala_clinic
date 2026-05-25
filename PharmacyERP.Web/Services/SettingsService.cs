using AutoMapper;
using PharmacyERP.Web.Interfaces;
using PharmacyERP.Web.Models.Entities;
using PharmacyERP.Web.Models.ViewModels;
using System.Diagnostics;
using System.IO;

namespace PharmacyERP.Web.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ISettingRepository _settingsRepo;
        private readonly IMapper _mapper;
        private readonly ILogger<SettingsService> _logger;

        public SettingsService(ISettingRepository settingsRepo, IMapper mapper, ILogger<SettingsService> logger)
        {
            _settingsRepo = settingsRepo;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<SettingsViewModel> GetSettingsAsync()
        {
            var settingsList = await _settingsRepo.GetAllAsync();
            var setting = settingsList.FirstOrDefault();

            if (setting == null)
            {
                // Create default settings if none exist
                setting = new Setting();
                await _settingsRepo.CreateAsync(setting);
            }

            return _mapper.Map<SettingsViewModel>(setting);
        }

        public async Task<bool> UpdateSettingsAsync(SettingsViewModel model, string webRootPath)
        {
            var settingsList = await _settingsRepo.GetAllAsync();
            var existingSetting = settingsList.FirstOrDefault();

            if (existingSetting == null) return false;

            // Handle Logo Upload
            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(webRootPath, "uploads", "store");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = "store_logo_" + Guid.NewGuid().ToString().Substring(0, 8) + Path.GetExtension(model.LogoFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.LogoFile.CopyToAsync(fileStream);
                }

                model.LogoPath = "/uploads/store/" + uniqueFileName;
            }
            else
            {
                // Preserve existing logo if no new file is uploaded
                model.LogoPath = existingSetting.Store.LogoPath;
            }

            _mapper.Map(model, existingSetting);
            existingSetting.UpdatedAt = DateTime.UtcNow;

            await _settingsRepo.UpdateAsync(existingSetting.Id!, existingSetting);
            return true;
        }

        public async Task<bool> TriggerBackupAsync()
        {
            try
            {
                var settingsList = await _settingsRepo.GetAllAsync();
                var setting = settingsList.FirstOrDefault();
                
                if (setting == null || string.IsNullOrEmpty(setting.Backup.BackupPath))
                {
                    _logger.LogWarning("Backup path not configured.");
                    return false;
                }

                string backupDir = Path.Combine(setting.Backup.BackupPath, $"Backup_{DateTime.Now:yyyyMMdd_HHmmss}");
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                // Construct mongodump command
                // NOTE: mongodump must be installed and in the system's PATH
                var startInfo = new ProcessStartInfo
                {
                    FileName = "mongodump",
                    Arguments = $"--uri=\"{setting.Backup.MongoDbConnectionString}\" --db=\"{setting.Backup.DatabaseName}\" --out=\"{backupDir}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0)
                    {
                        _logger.LogInformation($"Database backup successfully created at: {backupDir}");
                        return true;
                    }
                    else
                    {
                        string error = await process.StandardError.ReadToEndAsync();
                        _logger.LogError($"Mongodump failed: {error}");
                        return false;
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute automated backup.");
                return false;
            }
        }
    }
}

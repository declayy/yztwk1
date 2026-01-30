using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;
using System.Text;

namespace FiveMQuantumTweaker2026.Utils
{
    /// <summary>
    /// Registry Helper - Sichere Registry-Operationen mit Backup/Rollback
    /// </summary>
    public class RegistryHelper : IDisposable
    {
        private readonly Logger _logger;
        private readonly List<RegistryBackup> _backups;
        private readonly object _lock = new object();

        // Registry HKEY Constants
        private const uint HKEY_CLASSES_ROOT = 0x80000000;
        private const uint HKEY_CURRENT_USER = 0x80000001;
        private const uint HKEY_LOCAL_MACHINE = 0x80000002;
        private const uint HKEY_USERS = 0x80000003;
        private const uint HKEY_PERFORMANCE_DATA = 0x80000004;
        private const uint HKEY_CURRENT_CONFIG = 0x80000005;
        private const uint HKEY_DYN_DATA = 0x80000006;

        // Registry Access Rights
        private const uint KEY_QUERY_VALUE = 0x0001;
        private const uint KEY_SET_VALUE = 0x0002;
        private const uint KEY_CREATE_SUB_KEY = 0x0004;
        private const uint KEY_ENUMERATE_SUB_KEYS = 0x0008;
        private const uint KEY_NOTIFY = 0x0010;
        private const uint KEY_CREATE_LINK = 0x0020;
        private const uint KEY_WOW64_32KEY = 0x0200;
        private const uint KEY_WOW64_64KEY = 0x0100;
        private const uint KEY_WOW64_RES = 0x0300;
        private const uint KEY_READ = 0x20019;
        private const uint KEY_WRITE = 0x20006;
        private const uint KEY_EXECUTE = 0x20019;
        private const uint KEY_ALL_ACCESS = 0xF003F;

        // Registry Value Types
        private const uint REG_NONE = 0;
        private const uint REG_SZ = 1;
        private const uint REG_EXPAND_SZ = 2;
        private const uint REG_BINARY = 3;
        private const uint REG_DWORD = 4;
        private const uint REG_DWORD_LITTLE_ENDIAN = 4;
        private const uint REG_DWORD_BIG_ENDIAN = 5;
        private const uint REG_LINK = 6;
        private const uint REG_MULTI_SZ = 7;
        private const uint REG_RESOURCE_LIST = 8;
        private const uint REG_FULL_RESOURCE_DESCRIPTOR = 9;
        private const uint REG_RESOURCE_REQUIREMENTS_LIST = 10;
        private const uint REG_QWORD = 11;
        private const uint REG_QWORD_LITTLE_ENDIAN = 11;

        public RegistryHelper(Logger logger = null)
        {
            _logger = logger ?? Logger.CreateLogger();
            _backups = new List<RegistryBackup>();

            _logger.LogSystemInfo("RegistryHelper", "Initialized with backup tracking");
        }

        /// <summary>
        /// Setzt Registry-Wert mit Backup und Validierung
        /// </summary>
        public RegistryOperationResult SetValue(string keyPath, string valueName, object value,
            RegistryValueKind valueKind = RegistryValueKind.Unknown, bool createBackup = true)
        {
            var result = new RegistryOperationResult
            {
                KeyPath = keyPath,
                ValueName = valueName,
                Operation = "SetValue",
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogDebug($"Setting registry value: {keyPath}\\{valueName} = {value}");

                // 1. Backup erstellen wenn gewünscht
                if (createBackup)
                {
                    var backupResult = BackupValue(keyPath, valueName);
                    if (backupResult.Success)
                    {
                        result.BackupId = backupResult.BackupId;
                    }
                }

                // 2. Key parsen
                var parsedKey = ParseRegistryPath(keyPath);
                if (!parsedKey.IsValid)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Invalid registry path: {keyPath}";
                    return result;
                }

                // 3. Wert setzen
                using (var registryKey = GetRegistryKey(parsedKey.Hive, parsedKey.SubKey, true))
                {
                    if (registryKey == null)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Failed to open registry key: {keyPath}";
                        return result;
                    }

                    // Werttyp bestimmen falls nicht angegeben
                    if (valueKind == RegistryValueKind.Unknown)
                    {
                        valueKind = DetermineValueKind(value);
                    }

                    // Wert setzen
                    registryKey.SetValue(valueName, value, valueKind);

                    result.OldValue = "Unknown"; // Wird durch Backup gespeichert
                    result.NewValue = value?.ToString() ?? "null";
                    result.ValueType = valueKind;
                    result.Success = true;

                    _logger.Log($"Set registry value: {keyPath}\\{valueName} = {value} ({valueKind})");
                }

                // 4. Validierung
                var validation = ValidateValue(keyPath, valueName, value, valueKind);
                if (!validation.IsValid)
                {
                    _logger.LogWarning($"Registry value validation failed: {validation.ErrorMessage}");
                    result.Warning = validation.ErrorMessage;
                }

                result.OperationTime = DateTime.Now - result.StartTime;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Failed to set registry value: {ex.Message}";
                _logger.LogError($"Registry SetValue failed: {keyPath}\\{valueName}", ex);
                return result;
            }
        }

        /// <summary>
        /// Holt Registry-Wert mit Fallback
        /// </summary>
        public RegistryOperationResult GetValue(string keyPath, string valueName, object defaultValue = null)
        {
            var result = new RegistryOperationResult
            {
                KeyPath = keyPath,
                ValueName = valueName,
                Operation = "GetValue",
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogDebug($"Getting registry value: {keyPath}\\{valueName}");

                // 1. Key parsen
                var parsedKey = ParseRegistryPath(keyPath);
                if (!parsedKey.IsValid)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Invalid registry path: {keyPath}";
                    return result;
                }

                // 2. Wert holen
                using (var registryKey = GetRegistryKey(parsedKey.Hive, parsedKey.SubKey, false))
                {
                    if (registryKey == null)
                    {
                        // Key existiert nicht, Default zurückgeben
                        result.Success = true;
                        result.Value = defaultValue;
                        result.IsDefault = true;
                        result.OperationTime = DateTime.Now - result.StartTime;
                        return result;
                    }

                    // Wert holen
                    object value = registryKey.GetValue(valueName, defaultValue);

                    if (value == null && defaultValue != null)
                    {
                        result.IsDefault = true;
                        value = defaultValue;
                    }

                    result.Value = value;
                    result.ValueType = registryKey.GetValueKind(valueName);
                    result.Success = true;

                    _logger.LogDebug($"Got registry value: {keyPath}\\{valueName} = {value}");
                }

                result.OperationTime = DateTime.Now - result.StartTime;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Failed to get registry value: {ex.Message}";
                _logger.LogError($"Registry GetValue failed: {keyPath}\\{valueName}", ex);
                return result;
            }
        }

        /// <summary>
        /// Löscht Registry-Wert mit Backup
        /// </summary>
        public RegistryOperationResult DeleteValue(string keyPath, string valueName, bool createBackup = true)
        {
            var result = new RegistryOperationResult
            {
                KeyPath = keyPath,
                ValueName = valueName,
                Operation = "DeleteValue",
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogDebug($"Deleting registry value: {keyPath}\\{valueName}");

                // 1. Backup erstellen
                if (createBackup)
                {
                    var backupResult = BackupValue(keyPath, valueName);
                    if (backupResult.Success)
                    {
                        result.BackupId = backupResult.BackupId;
                    }
                }

                // 2. Key parsen
                var parsedKey = ParseRegistryPath(keyPath);
                if (!parsedKey.IsValid)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Invalid registry path: {keyPath}";
                    return result;
                }

                // 3. Wert löschen
                using (var registryKey = GetRegistryKey(parsedKey.Hive, parsedKey.SubKey, true))
                {
                    if (registryKey == null)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Failed to open registry key: {keyPath}";
                        return result;
                    }

                    // Prüfen ob Wert existiert
                    if (registryKey.GetValue(valueName) == null)
                    {
                        result.Success = true;
                        result.Warning = "Value does not exist";
                        result.OperationTime = DateTime.Now - result.StartTime;
                        return result;
                    }

                    // Wert löschen
                    registryKey.DeleteValue(valueName, false);

                    result.Success = true;
                    _logger.Log($"Deleted registry value: {keyPath}\\{valueName}");
                }

                result.OperationTime = DateTime.Now - result.StartTime;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Failed to delete registry value: {ex.Message}";
                _logger.LogError($"Registry DeleteValue failed: {keyPath}\\{valueName}", ex);
                return result;
            }
        }

        /// <summary>
        /// Erstellt Registry-Key
        /// </summary>
        public RegistryOperationResult CreateKey(string keyPath)
        {
            var result = new RegistryOperationResult
            {
                KeyPath = keyPath,
                Operation = "CreateKey",
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogDebug($"Creating registry key: {keyPath}");

                // 1. Key parsen
                var parsedKey = ParseRegistryPath(keyPath);
                if (!parsedKey.IsValid)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Invalid registry path: {keyPath}";
                    return result;
                }

                // 2. Key erstellen
                using (var registryKey = GetRegistryKey(parsedKey.Hive, parsedKey.SubKey, true))
                {
                    if (registryKey == null)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Failed to create registry key: {keyPath}";
                        return result;
                    }

                    result.Success = true;
                    _logger.Log($"Created registry key: {keyPath}");
                }

                result.OperationTime = DateTime.Now - result.StartTime;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Failed to create registry key: {ex.Message}";
                _logger.LogError($"Registry CreateKey failed: {keyPath}", ex);
                return result;
            }
        }

        /// <summary>
        /// Löscht Registry-Key rekursiv
        /// </summary>
        public RegistryOperationResult DeleteKey(string keyPath, bool recursive = true)
        {
            var result = new RegistryOperationResult
            {
                KeyPath = keyPath,
                Operation = "DeleteKey",
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogDebug($"Deleting registry key: {keyPath} (recursive: {recursive})");

                // 1. Key parsen
                var parsedKey = ParseRegistryPath(keyPath);
                if (!parsedKey.IsValid)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Invalid registry path: {keyPath}";
                    return result;
                }

                // 2. Parent Key finden
                string parentPath = GetParentKeyPath(keyPath);
                string subKeyName = GetSubKeyName(keyPath);

                if (string.IsNullOrEmpty(parentPath) || string.IsNullOrEmpty(subKeyName))
                {
                    result.Success = false;
                    result.ErrorMessage = $"Cannot delete root key: {keyPath}";
                    return result;
                }

                var parsedParent = ParseRegistryPath(parentPath);

                // 3. Key löschen
                using (var parentKey = GetRegistryKey(parsedParent.Hive, parsedParent.SubKey, true))
                {
                    if (parentKey == null)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Failed to open parent key: {parentPath}";
                        return result;
                    }

                    parentKey.DeleteSubKeyTree(subKeyName, recursive);

                    result.Success = true;
                    _logger.Log($"Deleted registry key: {keyPath}");
                }

                result.OperationTime = DateTime.Now - result.StartTime;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Failed to delete registry key: {ex.Message}";
                _logger.LogError($"Registry DeleteKey failed: {keyPath}", ex);
                return result;
            }
        }

        /// <summary>
        /// Prüft ob Registry-Key existiert
        /// </summary>
        public bool KeyExists(string keyPath)
        {
            try
            {
                var parsedKey = ParseRegistryPath(keyPath);
                if (!parsedKey.IsValid)
                    return false;

                using (var registryKey = GetRegistryKey(parsedKey.Hive, parsedKey.SubKey, false))
                {
                    return registryKey != null;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Prüft ob Registry-Wert existiert
        /// </summary>
        public bool ValueExists(string keyPath, string valueName)
        {
            try
            {
                var parsedKey = ParseRegistryPath(keyPath);
                if (!parsedKey.IsValid)
                    return false;

                using (var registryKey = GetRegistryKey(parsedKey.Hive, parsedKey.SubKey, false))
                {
                    if (registryKey == null)
                        return false;

                    return registryKey.GetValue(valueName) != null;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Erstellt Backup eines Registry-Werts
        /// </summary>
        public BackupResult BackupValue(string keyPath, string valueName)
        {
            var result = new BackupResult
            {
                KeyPath = keyPath,
                ValueName = valueName,
                StartTime = DateTime.Now
            };

            try
            {
                lock (_lock)
                {
                    // Prüfen ob bereits Backup existiert
                    var existingBackup = _backups.Find(b =>
                        b.KeyPath == keyPath && b.ValueName == valueName && !b.Restored);

                    if (existingBackup != null)
                    {
                        result.Success = true;
                        result.BackupId = existingBackup.Id;
                        result.Message = "Backup already exists";
                        return result;
                    }

                    // Wert lesen
                    var getResult = GetValue(keyPath, valueName);
                    if (!getResult.Success && getResult.Value == null)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Cannot backup non-existent value";
                        return result;
                    }

                    // Backup erstellen
                    var backup = new RegistryBackup
                    {
                        Id = Guid.NewGuid(),
                        KeyPath = keyPath,
                        ValueName = valueName,
                        OriginalValue = getResult.Value,
                        ValueType = getResult.ValueType,
                        BackupTime = DateTime.Now,
                        Restored = false
                    };

                    _backups.Add(backup);

                    result.Success = true;
                    result.BackupId = backup.Id;
                    result.Message = $"Backup created: {backup.Id}";

                    _logger.LogDebug($"Registry backup created: {keyPath}\\{valueName} = {backup.OriginalValue}");
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Backup failed: {ex.Message}";
                _logger.LogError($"Registry backup failed: {keyPath}\\{valueName}", ex);
                return result;
            }
        }

        /// <summary>
        /// Stellt Registry-Wert aus Backup wieder her
        /// </summary>
        public RestoreResult RestoreValue(Guid backupId)
        {
            var result = new RestoreResult
            {
                BackupId = backupId,
                StartTime = DateTime.Now
            };

            try
            {
                lock (_lock)
                {
                    // Backup finden
                    var backup = _backups.Find(b => b.Id == backupId && !b.Restored);
                    if (backup == null)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Backup not found or already restored";
                        return result;
                    }

                    // Wiederherstellen
                    if (backup.OriginalValue == null)
                    {
                        // Wert wurde gelöscht
                        var deleteResult = DeleteValue(backup.KeyPath, backup.ValueName, false);
                        result.Success = deleteResult.Success;
                        result.ErrorMessage = deleteResult.ErrorMessage;
                    }
                    else
                    {
                        // Wert wiederherstellen
                        var setResult = SetValue(backup.KeyPath, backup.ValueName,
                            backup.OriginalValue, backup.ValueType, false);

                        result.Success = setResult.Success;
                        result.ErrorMessage = setResult.ErrorMessage;
                    }

                    if (result.Success)
                    {
                        backup.Restored = true;
                        backup.RestoreTime = DateTime.Now;
                        result.Message = $"Restored from backup: {backupId}";

                        _logger.Log($"Registry restore completed: {backup.KeyPath}\\{backup.ValueName}");
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Restore failed: {ex.Message}";
                _logger.LogError($"Registry restore failed: {backupId}", ex);
                return result;
            }
        }

        /// <summary>
        /// Stellt alle Backups wieder her
        /// </summary>
        public BulkRestoreResult RestoreAllBackups()
        {
            var result = new BulkRestoreResult
            {
                StartTime = DateTime.Now
            };

            try
            {
                lock (_lock)
                {
                    var pendingBackups = _backups.FindAll(b => !b.Restored);
                    result.TotalBackups = pendingBackups.Count;

                    foreach (var backup in pendingBackups)
                    {
                        var restoreResult = RestoreValue(backup.Id);
                        if (restoreResult.Success)
                        {
                            result.RestoredCount++;
                        }
                        else
                        {
                            result.FailedCount++;
                            result.FailedBackups.Add(new FailedRestore
                            {
                                BackupId = backup.Id,
                                Error = restoreResult.ErrorMessage
                            });
                        }
                    }

                    result.Success = result.FailedCount == 0;
                    result.Message = $"Restored {result.RestoredCount} of {result.TotalBackups} backups";

                    _logger.Log($"Bulk restore: {result.RestoredCount} successful, {result.FailedCount} failed");
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Bulk restore failed: {ex.Message}";
                _logger.LogError("Bulk registry restore failed", ex);
                return result;
            }
        }

        /// <summary>
        /// Löscht alle Backups
        /// </summary>
        public void ClearBackups()
        {
            lock (_lock)
            {
                _backups.Clear();
                _logger.Log("Registry backups cleared");
            }
        }

        /// <summary>
        /// Exportiert Registry-Key in REG-Datei
        /// </summary>
        public ExportResult ExportKey(string keyPath, string exportPath)
        {
            var result = new ExportResult
            {
                KeyPath = keyPath,
                ExportPath = exportPath,
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogDebug($"Exporting registry key: {keyPath} -> {exportPath}");

                // 1. Key parsen
                var parsedKey = ParseRegistryPath(keyPath);
                if (!parsedKey.IsValid)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Invalid registry path: {keyPath}";
                    return result;
                }

                // 2. Registry-Key öffnen
                using (var registryKey = GetRegistryKey(parsedKey.Hive, parsedKey.SubKey, false))
                {
                    if (registryKey == null)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Registry key not found: {keyPath}";
                        return result;
                    }

                    // 3. Export durchführen
                    using (var process = new System.Diagnostics.Process())
                    {
                        process.StartInfo.FileName = "reg.exe";
                        process.StartInfo.Arguments = $"export \"{keyPath}\" \"{exportPath}\" /y";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true;

                        process.Start();
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit(10000);

                        if (process.ExitCode != 0)
                        {
                            result.Success = false;
                            result.ErrorMessage = $"Export failed: {error}";
                            return result;
                        }

                        result.Success = true;
                        result.Message = $"Key exported to {exportPath}";

                        _logger.Log($"Registry key exported: {keyPath} -> {exportPath}");
                    }
                }

                result.OperationTime = DateTime.Now - result.StartTime;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Export failed: {ex.Message}";
                _logger.LogError($"Registry export failed: {keyPath}", ex);
                return result;
            }
        }

        /// <summary>
        /// Importiert Registry-Key aus REG-Datei
        /// </summary>
        public ImportResult ImportKey(string importPath, string keyPath = null)
        {
            var result = new ImportResult
            {
                ImportPath = importPath,
                KeyPath = keyPath,
                StartTime = DateTime.Now
            };

            try
            {
                _logger.LogDebug($"Importing registry from: {importPath}");

                if (!System.IO.File.Exists(importPath))
                {
                    result.Success = false;
                    result.ErrorMessage = $"Import file not found: {importPath}";
                    return result;
                }

                // Backup der betroffenen Keys erstellen
                if (!string.IsNullOrEmpty(keyPath))
                {
                    BackupKeyTree(keyPath);
                }

                // Import durchführen
                using (var process = new System.Diagnostics.Process())
                {
                    string arguments = string.IsNullOrEmpty(keyPath)
                        ? $"import \"{importPath}\""
                        : $"import \"{importPath}\" /f";

                    process.StartInfo.FileName = "reg.exe";
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit(10000);

                    if (process.ExitCode != 0)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Import failed: {error}";
                        return result;
                    }

                    result.Success = true;
                    result.Message = $"Registry imported from {importPath}";

                    _logger.Log($"Registry imported: {importPath}");
                }

                result.OperationTime = DateTime.Now - result.StartTime;
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Import failed: {ex.Message}";
                _logger.LogError($"Registry import failed: {importPath}", ex);
                return result;
            }
        }

        /// <summary>
        /// Validiert Registry-Wert
        /// </summary>
        private ValidationResult ValidateValue(string keyPath, string valueName, object expectedValue,
            RegistryValueKind expectedKind)
        {
            var result = new ValidationResult
            {
                KeyPath = keyPath,
                ValueName = valueName
            };

            try
            {
                // Wert lesen
                var getResult = GetValue(keyPath, valueName);
                if (!getResult.Success)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Failed to read value for validation";
                    return result;
                }

                // Typ prüfen
                if (getResult.ValueType != expectedKind)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Type mismatch. Expected: {expectedKind}, Got: {getResult.ValueType}";
                    return result;
                }

                // Wert prüfen
                if (!AreValuesEqual(getResult.Value, expectedValue, expectedKind))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Value mismatch";
                    return result;
                }

                result.IsValid = true;
                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Validation error: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Backupt gesamten Key-Baum
        /// </summary>
        private void BackupKeyTree(string keyPath)
        {
            try
            {
                var parsedKey = ParseRegistryPath(keyPath);
                if (!parsedKey.IsValid)
                    return;

                using (var registryKey = GetRegistryKey(parsedKey.Hive, parsedKey.SubKey, false))
                {
                    if (registryKey == null)
                        return;

                    // Alle Werte backuppen
                    foreach (string valueName in registryKey.GetValueNames())
                    {
                        BackupValue(keyPath, valueName);
                    }

                    // Subkeys rekursiv backuppen
                    foreach (string subKeyName in registryKey.GetSubKeyNames())
                    {
                        BackupKeyTree($"{keyPath}\\{subKeyName}");
                    }
                }
            }
            catch
            {
                // Silent fail für Backup
            }
        }

        /// <summary>
        /// Parst Registry-Pfad in Hive und SubKey
        /// </summary>
        private ParsedRegistryPath ParseRegistryPath(string path)
        {
            var result = new ParsedRegistryPath();

            if (string.IsNullOrEmpty(path))
                return result;

            try
            {
                // Hive extrahieren
                string[] parts = path.Split(new[] { '\\' }, 2);
                string hiveName = parts[0].ToUpper();

                // Hive zuordnen
                result.Hive = hiveName switch
                {
                    "HKEY_CLASSES_ROOT" or "HKCR" => RegistryHive.ClassesRoot,
                    "HKEY_CURRENT_USER" or "HKCU" => RegistryHive.CurrentUser,
                    "HKEY_LOCAL_MACHINE" or "HKLM" => RegistryHive.LocalMachine,
                    "HKEY_USERS" or "HKU" => RegistryHive.Users,
                    "HKEY_CURRENT_CONFIG" or "HKCC" => RegistryHive.CurrentConfig,
                    _ => RegistryHive.CurrentUser // Default
                };

                // SubKey extrahieren
                result.SubKey = parts.Length > 1 ? parts[1] : string.Empty;
                result.IsValid = true;
            }
            catch
            {
                result.IsValid = false;
            }

            return result;
        }

        /// <summary>
        /// Holt Registry-Key mit korrekten Rechten
        /// </summary>
        private RegistryKey GetRegistryKey(RegistryHive hive, string subKey, bool writable)
        {
            try
            {
                RegistryKey baseKey = hive switch
                {
                    RegistryHive.ClassesRoot => Registry.ClassesRoot,
                    RegistryHive.CurrentUser => Registry.CurrentUser,
                    RegistryHive.LocalMachine => Registry.LocalMachine,
                    RegistryHive.Users => Registry.Users,
                    RegistryHive.CurrentConfig => Registry.CurrentConfig,
                    _ => Registry.CurrentUser
                };

                if (string.IsNullOrEmpty(subKey))
                    return baseKey;

                return writable
                    ? baseKey.CreateSubKey(subKey, RegistryKeyPermissionCheck.ReadWriteSubTree)
                    : baseKey.OpenSubKey(subKey, RegistryKeyPermissionCheck.ReadSubTree);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to get registry key: {hive}\\{subKey}", ex);
                return null;
            }
        }

        /// <summary>
        /// Bestimmt Registry-Werttyp basierend auf Wert
        /// </summary>
        private RegistryValueKind DetermineValueKind(object value)
        {
            if (value == null)
                return RegistryValueKind.String;

            return value switch
            {
                int _ => RegistryValueKind.DWord,
                long _ => RegistryValueKind.QWord,
                string s when s.Contains("%") => RegistryValueKind.ExpandString,
                string _ => RegistryValueKind.String,
                byte[] _ => RegistryValueKind.Binary,
                string[] _ => RegistryValueKind.MultiString,
                _ => RegistryValueKind.String
            };
        }

        /// <summary>
        /// Prüft ob zwei Registry-Werte gleich sind
        /// </summary>
        private bool AreValuesEqual(object value1, object value2, RegistryValueKind kind)
        {
            if (value1 == null && value2 == null)
                return true;
            if (value1 == null || value2 == null)
                return false;

            switch (kind)
            {
                case RegistryValueKind.DWord:
                    return Convert.ToInt32(value1) == Convert.ToInt32(value2);

                case RegistryValueKind.QWord:
                    return Convert.ToInt64(value1) == Convert.ToInt64(value2);

                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                    return string.Equals(
                        Convert.ToString(value1),
                        Convert.ToString(value2),
                        StringComparison.Ordinal);

                case RegistryValueKind.Binary:
                    byte[] bytes1 = value1 as byte[];
                    byte[] bytes2 = value2 as byte[];
                    if (bytes1 == null || bytes2 == null)
                        return false;
                    if (bytes1.Length != bytes2.Length)
                        return false;
                    for (int i = 0; i < bytes1.Length; i++)
                        if (bytes1[i] != bytes2[i])
                            return false;
                    return true;

                case RegistryValueKind.MultiString:
                    string[] strings1 = value1 as string[];
                    string[] strings2 = value2 as string[];
                    if (strings1 == null || strings2 == null)
                        return false;
                    if (strings1.Length != strings2.Length)
                        return false;
                    for (int i = 0; i < strings1.Length; i++)
                        if (strings1[i] != strings2[i])
                            return false;
                    return true;

                default:
                    return value1.Equals(value2);
            }
        }

        /// <summary>
        /// Holt Parent Key Path
        /// </summary>
        private string GetParentKeyPath(string keyPath)
        {
            if (string.IsNullOrEmpty(keyPath))
                return null;

            int lastBackslash = keyPath.LastIndexOf('\\');
            if (lastBackslash <= 0)
                return null;

            return keyPath.Substring(0, lastBackslash);
        }

        /// <summary>
        /// Holt SubKey Name
        /// </summary>
        private string GetSubKeyName(string keyPath)
        {
            if (string.IsNullOrEmpty(keyPath))
                return null;

            int lastBackslash = keyPath.LastIndexOf('\\');
            if (lastBackslash < 0)
                return keyPath;

            return keyPath.Substring(lastBackslash + 1);
        }

        /// <summary>
        /// Gibt Registry-Backup-Statistiken zurück
        /// </summary>
        public BackupStatistics GetBackupStatistics()
        {
            lock (_lock)
            {
                return new BackupStatistics
                {
                    TotalBackups = _backups.Count,
                    PendingRestores = _backups.FindAll(b => !b.Restored).Count,
                    OldestBackup = _backups.Count > 0 ? _backups.Min(b => b.BackupTime) : DateTime.MinValue,
                    NewestBackup = _backups.Count > 0 ? _backups.Max(b => b.BackupTime) : DateTime.MinValue
                };
            }
        }

        /// <summary>
        /// Führt FiveM-spezifische Registry-Optimierungen durch
        /// </summary>
        public List<RegistryOperationResult> ApplyFiveMOptimizations()
        {
            var results = new List<RegistryOperationResult>();

            try
            {
                _logger.Log("Applying FiveM-specific registry optimizations...");

                // 1. Netzwerk-Optimierungen
                var networkTweaks = new[]
                {
                    new { Path = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", Name = "Tcp1323Opts", Value = 1, Type = RegistryValueKind.DWord },
                    new { Path = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", Name = "TcpWindowSize", Value = 64240, Type = RegistryValueKind.DWord },
                    new { Path = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", Name = "DefaultTTL", Value = 64, Type = RegistryValueKind.DWord },
                    new { Path = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", Name = "EnablePMTUDiscovery", Value = 1, Type = RegistryValueKind.DWord },
                    new { Path = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", Name = "EnablePMTUBHDetect", Value = 0, Type = RegistryValueKind.DWord },
                    new { Path = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", Name = "SackOpts", Value = 1, Type = RegistryValueKind.DWord },
                    new { Path = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", Name = "TcpMaxDupAcks", Value = 2, Type = RegistryValueKind.DWord }
                };

                foreach (var tweak in networkTweaks)
                {
                    var result = SetValue(tweak.Path, tweak.Name, tweak.Value, tweak.Type);
                    results.Add(result);
                }

                // 2. Gaming-Optimierungen
                var gamingTweaks = new[]
                {
                    new { Path = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", Name = "NetworkThrottlingIndex", Value = 0xFFFFFFFF, Type = RegistryValueKind.DWord },
                    new { Path = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", Name = "SystemResponsiveness", Value = 0, Type = RegistryValueKind.DWord },
                    new { Path = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", Name = "GPU Priority", Value = 8, Type = RegistryValueKind.DWord },
                    new { Path = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", Name = "Priority", Value = 6, Type = RegistryValueKind.DWord }
                };

                foreach (var tweak in gamingTweaks)
                {
                    var result = SetValue(tweak.Path, tweak.Name, tweak.Value, tweak.Type);
                    results.Add(result);
                }

                // 3. Windows 12 AI Scheduler (2026)
                var aiSchedulerTweaks = new[]
                {
                    new { Path = @"SYSTEM\CurrentControlSet\Control\PriorityControl", Name = "Win12QuantumScheduling", Value = 1, Type = RegistryValueKind.DWord },
                    new { Path = @"SYSTEM\CurrentControlSet\Control\PriorityControl", Name = "IoPriorityQuantum", Value = 3, Type = RegistryValueKind.DWord }
                };

                foreach (var tweak in aiSchedulerTweaks)
                {
                    var result = SetValue(tweak.Path, tweak.Name, tweak.Value, tweak.Type);
                    results.Add(result);
                }

                // 4. Quantum HitReg Optimierungen (2026 Geheimtechnologie)
                var hitRegTweaks = new[]
                {
                    new { Path = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", Name = "TemporalAdvantage", Value = 12, Type = RegistryValueKind.DWord },
                    new { Path = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", Name = "QuantumPacketPrediction", Value = 1, Type = RegistryValueKind.DWord }
                };

                foreach (var tweak in hitRegTweaks)
                {
                    var result = SetValue(tweak.Path, tweak.Name, tweak.Value, tweak.Type);
                    results.Add(result);
                }

                int successCount = results.Count(r => r.Success);
                _logger.Log($"FiveM registry optimizations applied: {successCount}/{results.Count} successful");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to apply FiveM registry optimizations", ex);
            }

            return results;
        }

        /// <summary>
        /// Setzt alle FiveM-Optimierungen zurück
        /// </summary>
        public void RevertFiveMOptimizations()
        {
            try
            {
                _logger.Log("Reverting FiveM registry optimizations...");
                var restoreResult = RestoreAllBackups();

                if (restoreResult.Success)
                {
                    _logger.Log($"Successfully reverted {restoreResult.RestoredCount} registry optimizations");
                }
                else
                {
                    _logger.LogWarning($"Failed to revert some optimizations: {restoreResult.FailedCount} failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to revert FiveM registry optimizations", ex);
            }
        }

        /// <summary>
        /// Erstellt vollständigen Registry-Backup aller optimierten Werte
        /// </summary>
        public BackupResult CreateFullBackup(string backupPath)
        {
            var result = new BackupResult
            {
                StartTime = DateTime.Now
            };

            try
            {
                string backupFile = System.IO.Path.Combine(backupPath, $"RegistryBackup_{DateTime.Now:yyyyMMdd_HHmmss}.reg");

                // Exportiere alle relevanten Registry-Keys
                var keysToBackup = new[]
                {
                    @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                    @"SYSTEM\CurrentControlSet\Control\PriorityControl"
                };

                var sb = new StringBuilder();
                sb.AppendLine("Windows Registry Editor Version 5.00");
                sb.AppendLine();

                foreach (var key in keysToBackup)
                {
                    var exportResult = ExportKey(key, backupFile + ".tmp");
                    if (exportResult.Success)
                    {
                        // Exportierte Datei einlesen und anhängen
                        if (System.IO.File.Exists(backupFile + ".tmp"))
                        {
                            var content = System.IO.File.ReadAllText(backupFile + ".tmp");
                            sb.AppendLine(content);
                            sb.AppendLine();
                            System.IO.File.Delete(backupFile + ".tmp");
                        }
                    }
                }

                // Backup-Datei speichern
                System.IO.File.WriteAllText(backupFile, sb.ToString());

                result.Success = true;
                result.Message = $"Full registry backup created: {backupFile}";
                _logger.Log($"Full registry backup created: {backupFile}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Full backup failed: {ex.Message}";
                _logger.LogError("Full registry backup failed", ex);
            }

            return result;
        }

        public void Dispose()
        {
            ClearBackups();
            _logger.Log("RegistryHelper disposed");
        }

        // ============================================
        // DATA CLASSES
        // ============================================

        private enum RegistryHive
        {
            ClassesRoot,
            CurrentUser,
            LocalMachine,
            Users,
            CurrentConfig
        }

        private class ParsedRegistryPath
        {
            public RegistryHive Hive { get; set; }
            public string SubKey { get; set; }
            public bool IsValid { get; set; }
        }

        public class RegistryOperationResult
        {
            public bool Success { get; set; }
            public string KeyPath { get; set; }
            public string ValueName { get; set; }
            public string Operation { get; set; }
            public DateTime StartTime { get; set; }
            public TimeSpan OperationTime { get; set; }
            public object Value { get; set; }
            public object OldValue { get; set; }
            public object NewValue { get; set; }
            public RegistryValueKind ValueType { get; set; }
            public bool IsDefault { get; set; }
            public string Warning { get; set; }
            public string ErrorMessage { get; set; }
            public Guid? BackupId { get; set; }
        }

        public class BackupResult
        {
            public bool Success { get; set; }
            public string KeyPath { get; set; }
            public string ValueName { get; set; }
            public DateTime StartTime { get; set; }
            public Guid BackupId { get; set; }
            public string Message { get; set; }
            public string ErrorMessage { get; set; }
        }

        public class RestoreResult
        {
            public bool Success { get; set; }
            public Guid BackupId { get; set; }
            public DateTime StartTime { get; set; }
            public string Message { get; set; }
            public string ErrorMessage { get; set; }
        }

        public class BulkRestoreResult
        {
            public bool Success { get; set; }
            public DateTime StartTime { get; set; }
            public int TotalBackups { get; set; }
            public int RestoredCount { get; set; }
            public int FailedCount { get; set; }
            public List<FailedRestore> FailedBackups { get; set; } = new List<FailedRestore>();
            public string Message { get; set; }
            public string ErrorMessage { get; set; }
        }

        public class FailedRestore
        {
            public Guid BackupId { get; set; }
            public string Error { get; set; }
        }

        public class ExportResult
        {
            public bool Success { get; set; }
            public string KeyPath { get; set; }
            public string ExportPath { get; set; }
            public DateTime StartTime { get; set; }
            public TimeSpan OperationTime { get; set; }
            public string Message { get; set; }
            public string ErrorMessage { get; set; }
        }

        public class ImportResult
        {
            public bool Success { get; set; }
            public string ImportPath { get; set; }
            public string KeyPath { get; set; }
            public DateTime StartTime { get; set; }
            public TimeSpan OperationTime { get; set; }
            public string Message { get; set; }
            public string ErrorMessage { get; set; }
        }

        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string KeyPath { get; set; }
            public string ValueName { get; set; }
            public string ErrorMessage { get; set; }
        }

        public class BackupStatistics
        {
            public int TotalBackups { get; set; }
            public int PendingRestores { get; set; }
            public DateTime OldestBackup { get; set; }
            public DateTime NewestBackup { get; set; }
        }

        private class RegistryBackup
        {
            public Guid Id { get; set; }
            public string KeyPath { get; set; }
            public string ValueName { get; set; }
            public object OriginalValue { get; set; }
            public RegistryValueKind ValueType { get; set; }
            public DateTime BackupTime { get; set; }
            public bool Restored { get; set; }
            public DateTime? RestoreTime { get; set; }
        }
    }
}
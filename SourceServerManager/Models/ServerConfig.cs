using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using SourceServerManager.Services;

namespace SourceServerManager.Models;

public class ServerConfig : INotifyPropertyChanged
{
    private string _displayName = string.Empty; // This will be auto-populated with hostname
    private string _ipAddress = string.Empty;
    private int _rconPort = 27015;
    private string _rconPassword = string.Empty;
    private string _ftpHost = string.Empty;
    private int _ftpPort = 21;
    private string _ftpUsername = string.Empty;
    private string _ftpPassword = string.Empty;
    private string _ftpRootDirectory = string.Empty;
    private bool _isSelected = false;
    private bool _isOnline = false;
    private int _playersOnline = 0;
    private int _maxPlayers = 24;
    private string _currentMap = string.Empty;
    private string _serverHostname = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public enum FileTransferProtocol
    {
        FTP,
        SFTP
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    // This is a display name (used as fallback if hostname isn't available)
    [JsonPropertyName("name")]
    public string DisplayName
    {
        get => string.IsNullOrEmpty(_serverHostname) ? _displayName : _serverHostname;
        set => SetField(ref _displayName, value);
    }

    public string IpAddress
    {
        get => _ipAddress;
        set => SetField(ref _ipAddress, value);
    }

    public int RconPort
    {
        get => _rconPort;
        set => SetField(ref _rconPort, value);
    }

    [JsonIgnore]
    public string RconPassword
    {
        get 
        { 
            // If password is encrypted, decrypt it for UI usage
            if (!string.IsNullOrEmpty(_rconPassword) && EncryptionService.IsEncrypted(_rconPassword))
            {
                try
                {
                    return EncryptionService.DecryptString(_rconPassword);
                }
                catch
                {
                    // If decryption fails, return empty string
                    return string.Empty;
                }
            }
            // Return as-is (either plain text for migration, or empty)
            return _rconPassword;
        }
        set 
        {
            var currentDecrypted = RconPassword;
            if (!string.Equals(currentDecrypted, value))
            {
                // Encrypt and store the password
                if (!string.IsNullOrEmpty(value))
                {
                    try
                    {
                        _rconPassword = EncryptionService.EncryptString(value);
                    }
                    catch
                    {
                        // If encryption fails, store as plain text (fallback)
                        _rconPassword = value;
                    }
                }
                else
                {
                    _rconPassword = string.Empty;
                }
                OnPropertyChanged();
            }
        }
    }

    // JSON property - stores the encrypted password directly
    [JsonPropertyName("rconPassword")]
    public string RconPasswordStorage
    {
        get => _rconPassword;
        set => _rconPassword = value ?? string.Empty;
    }

    // FTP settings
    public string FtpHost
    {
        get => _ftpHost;
        set => SetField(ref _ftpHost, value);
    }

    public int FtpPort
    {
        get => _ftpPort;
        set => SetField(ref _ftpPort, value);
    }

    public string FtpUsername
    {
        get => _ftpUsername;
        set => SetField(ref _ftpUsername, value);
    }

    [JsonIgnore]
    public string FtpPassword
    {
        get 
        { 
            // If password is encrypted, decrypt it for UI usage
            if (!string.IsNullOrEmpty(_ftpPassword) && EncryptionService.IsEncrypted(_ftpPassword))
            {
                try
                {
                    return EncryptionService.DecryptString(_ftpPassword);
                }
                catch
                {
                    // If decryption fails, return empty string
                    return string.Empty;
                }
            }
            // Return as-is (either plain text for migration, or empty)
            return _ftpPassword;
        }
        set 
        {
            var currentDecrypted = FtpPassword;
            if (!string.Equals(currentDecrypted, value))
            {
                // Encrypt and store the password
                if (!string.IsNullOrEmpty(value))
                {
                    try
                    {
                        _ftpPassword = EncryptionService.EncryptString(value);
                    }
                    catch
                    {
                        // If encryption fails, store as plain text (fallback)
                        _ftpPassword = value;
                    }
                }
                else
                {
                    _ftpPassword = string.Empty;
                }
                OnPropertyChanged();
            }
        }
    }

    // JSON property - stores the encrypted password directly
    [JsonPropertyName("ftpPassword")]
    public string FtpPasswordStorage
    {
        get => _ftpPassword;
        set => _ftpPassword = value ?? string.Empty;
    }

    public string FtpRootDirectory
    {
        get => _ftpRootDirectory;
        set => SetField(ref _ftpRootDirectory, value);
    }

    public FileTransferProtocol FtpProtocol { get; set; } = FileTransferProtocol.FTP;

    // Store server hostname
    public string ServerHostname
    {
        get => _serverHostname;
        set
        {
            if (SetField(ref _serverHostname, value))
            {
                // Notify that DisplayName may have changed
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    // Server status - not persisted
    [JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    [JsonIgnore]
    public bool IsOnline
    {
        get => _isOnline;
        set
        {
            if (SetField(ref _isOnline, value))
            {
                OnPropertyChanged(nameof(PlayerMapInfo));
            }
        }
    }

    [JsonIgnore]
    public int PlayersOnline
    {
        get => _playersOnline;
        set
        {
            if (SetField(ref _playersOnline, value))
            {
                OnPropertyChanged(nameof(PlayerMapInfo));
            }
        }
    }

    [JsonIgnore]
    public int MaxPlayers
    {
        get => _maxPlayers;
        set
        {
            if (SetField(ref _maxPlayers, value))
            {
                OnPropertyChanged(nameof(PlayerMapInfo));
            }
        }
    }

    [JsonIgnore]
    public string CurrentMap
    {
        get => _currentMap;
        set
        {
            if (SetField(ref _currentMap, value))
            {
                OnPropertyChanged(nameof(PlayerMapInfo));
            }
        }
    }

    [JsonIgnore]
    public string PlayerMapInfo => $"{PlayersOnline}/{MaxPlayers} - {CurrentMap}";
}
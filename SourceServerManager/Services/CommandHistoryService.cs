using System.Collections.Generic;

namespace SourceServerManager.Services;

public class CommandHistoryService
{
    private readonly List<string> _commandHistory = [];
    private int _currentHistoryIndex = -1;

    public void AddCommand(string command)
    {
        // Don't add empty commands or duplicates of the last command
        if (string.IsNullOrWhiteSpace(command) ||
            (_commandHistory.Count > 0 && _commandHistory[0] == command))
        {
            return;
        }

        // Add to the beginning of the list
        _commandHistory.Insert(0, command);

        // Limit history size
        if (_commandHistory.Count > 100)
        {
            _commandHistory.RemoveAt(_commandHistory.Count - 1);
        }

        // Reset index
        _currentHistoryIndex = -1;
    }

    public string NavigateUp()
    {
        if (_commandHistory.Count == 0)
        {
            return string.Empty;
        }

        _currentHistoryIndex = System.Math.Min(_currentHistoryIndex + 1, _commandHistory.Count - 1);
        return _commandHistory[_currentHistoryIndex];
    }

    public string NavigateDown()
    {
        if (_commandHistory.Count == 0 || _currentHistoryIndex <= 0)
        {
            _currentHistoryIndex = -1;
            return string.Empty;
        }

        _currentHistoryIndex--;
        return _commandHistory[_currentHistoryIndex];
    }

    public void ResetNavigation()
    {
        _currentHistoryIndex = -1;
    }
}
/*
 * Avalon Mud Client
 *
 * @project lead      : Blake Pell
 * @website           : http://www.blakepell.com
 * @copyright         : Copyright (c), 2018-2021 All rights reserved.
 * @license           : MIT
 */

using System.Collections.Generic;
using Avalon.Common.Models;

namespace Avalon.Common.Scripting
{
    public interface IScriptCommands
    {
        /// <summary>
        /// Sends text to the server.
        /// </summary>
        /// <param name="cmd"></param>
        void Send(string cmd);

        /// <summary>
        /// Gets a <see cref="Variable"/> from the profile's global variable list.
        /// </summary>
        /// <param name="key"></param>
        string GetVariable(string key);

        /// <summary>
        /// Sets a <see cref="Variable"/> in the profile's global variable list.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void SetVariable(string key, string value);

        /// <summary>
        /// Sets a <see cref="Variable"/> in the profile's global variable list.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="color">A known color</param>
        void SetVariable(string key, string value, string color);

        /// <summary>
        /// Shows a variable in the variable repeater if the key is found.
        /// </summary>
        /// <param name="key"></param>
        void ShowVariable(string key);

        /// <summary>
        /// Hides a variable in the variable repeater if the key is found.
        /// </summary>
        /// <param name="key"></param>
        void HideVariable(string key);

        /// <summary>
        /// Removes a <see cref="Variable"/> from the global variable list.
        /// </summary>
        /// <param name="key"></param>
        void RemoveVariable(string key);

        /// <summary>
        /// Echos text to the main terminal.
        /// </summary>
        /// <param name="msg"></param>
        void Echo(string msg);

        /// <summary>
        /// Echos text to the main terminal.
        /// </summary>
        /// <param name="msg"></param>
        void Echo(string msg, string color, bool reverse);

        /// <summary>
        /// Echos an event to the main terminal.
        /// </summary>
        /// <param name="msg"></param>
        void EchoEvent(string msg);

        /// <summary>
        /// Echos text to a custom window.  The append parameter is true by default but if made
        /// false this will clear the text in the window first.
        /// </summary>
        /// <param name="windowName"></param>
        /// <param name="text"></param>
        void EchoWindow(string windowName, string text);

        /// <summary>
        /// Makes an info echo.
        /// </summary>
        /// <param name="msg"></param>
        void LogInfo(string msg, params object[] args);

        /// <summary>
        /// Makes an warning echo.
        /// </summary>
        /// <param name="msg"></param>
        void LogWarning(string msg, params object[] args);

        /// <summary>
        /// Makes an error echo.
        /// </summary>
        /// <param name="msg"></param>
        void LogError(string msg, params object[] args);

        /// <summary>
        /// Makes a success log echo.
        /// </summary>
        /// <param name="msg"></param>
        void LogSuccess(string msg, params object[] args);

        /// <summary>
        /// Clears the text in a terminal of a specified window name.
        /// </summary>
        /// <param name="windowName"></param>
        void ClearWindow(string windowName);

        /// <summary>
        /// Returns the first non null and non empty value.  If none are found a blank
        /// string will be returned.
        /// </summary>
        string Coalesce(string valueOne, string valueTwo);

        /// <summary>
        /// Returns the specified number of characters from the left side of the string.  If more
        /// characters were requested than exist the full string is returned.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="length"></param>
        string Left(string str, int length);

        /// <summary>
        /// Returns the specified number of characters from the right side of the string.  If more
        /// characters were requested than exist the full string is returned.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="length"></param>
        string Right(string str, int length);

        /// <summary>
        /// Returns the substring starting at the specified index.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="startIndex"></param>
        string Substring(string str, int startIndex);

        /// <summary>
        /// Returns the substring starting at the specified index for the specified length.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        string Substring(string str, int startIndex, int length);

        /// <summary>
        /// Returns the zero based index of the first occurrence of a string in another string.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="search"></param>
        int IndexOf(string str, string search);

        /// <summary>
        /// Returns the zero based index of the first occurrence of a string in another string.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="search"></param>
        /// <param name="start"></param>
        int IndexOf(string str, string search, int start);

        /// <summary>
        /// Returns the zero based index of the first occurrence of a string in another string.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="search"></param>
        /// <param name="start"></param>
        /// <param name="length"></param>
        int IndexOf(string str, string search, int start, int length);

        /// <summary>
        /// Returns the zero based index of the last occurrence of a string in another string.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="search"></param>
        int LastIndexOf(string str, string search);

        /// <summary>
        /// Returns the zero based index of the last occurrence of a string in another string.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="search"></param>
        /// <param name="start"></param>
        int LastIndexOf(string str, string search, int start);

        /// <summary>
        /// Returns the zero based index of the last occurrence of a string in another string.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="search"></param>
        /// <param name="start"></param>
        /// <param name="length"></param>
        int LastIndexOf(string str, string search, int start, int length);

        /// <summary>
        /// Returns the current time formatted as either 12-hour or 24-hour.
        /// </summary>
        /// <param name="meridiemTime">Whether or not to return the time in AM/PM format.</param>
        string GetTime(bool meridiemTime = false);

        /// <summary>
        /// Returns the current hour.
        /// </summary>]
        int GetHour();

        /// <summary>
        /// Returns the current minute.
        /// </summary>
        int GetMinute();

        /// <summary>
        /// Returns the current second.
        /// </summary>
        int GetSecond();

        /// <summary>
        /// Returns the current millisecond.
        /// </summary>
        int GetMillisecond();

        /// <summary>
        /// The minutes elapsed since the start of the day.
        /// </summary>
        int DailyMinutesElapsed();

        /// <summary>
        /// The seconds elapsed since the start of the day.
        /// </summary>
        int DailySecondsElapsed();

        /// <summary>
        /// The milliseconds elapsed since the start of the day.
        /// </summary>
        int DailyMillisecondsElapsed();

        /// <summary>
        /// Will pause the Lua script for the designated amount of milliseconds.  This is not async
        /// so it will block the Lua (but since Lua is called async the rest of the program continues
        /// to work).  This will be an incredibly useful and powerful command for those crafting Lua scripts.
        /// </summary>
        /// <param name="milliseconds"></param>
        void Sleep(int milliseconds);

        /// <summary>
        /// Returns a random number.
        /// </summary>
        /// <param name="low"></param>
        /// <param name="high"></param>
        int RandomNumber(int low, int high);

        /// <summary>
        /// Returns a random element from the string array provided.
        /// </summary>
        /// <param name="choices"></param>
        string RandomChoice(string[] choices);

        /// <summary>
        /// Returns a random element from the string provided that string and a delimiter.  The delimiter
        /// is used to split the string into the choices.
        /// </summary>
        /// <param name="choices"></param>
        /// <param name="delimiter"></param>
        string RandomChoice(string choices, string delimiter);

        /// <summary>
        /// Returns a new GUID.
        /// </summary>
        string Guid();

        /// <summary>
        /// Sets the mud client's title.
        /// </summary>
        /// <param name="title"></param>
        void SetTitle(string title);

        /// <summary>
        /// The text that is currently in the scrape buffer.
        /// </summary>
        string GetScrapedText();

        /// <summary>
        /// Turns text capturing on.
        /// </summary>
        void CaptureOn();

        /// <summary>
        /// Turns text capturing off.
        /// </summary>
        void CaptureOff();

        /// <summary>
        /// Clears the text capturing buffer.
        /// </summary>
        void CaptureClear();

        /// <summary>
        /// Checks if a string exists in another string (Case Sensitive).
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="contains"></param>
        bool Contains(string buf, string contains);

        /// <summary>
        /// Checks if a string exists in another string.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="contains"></param>
        /// <param name="ignoreCase"></param>
        bool Contains(string buf, string contains, bool ignoreCase);

        /// <summary>
        /// Removes non alpha characters but allows for an exceptions list of chars to be provided that
        /// should be included.
        /// </summary>
        /// <param name="buf">The string to remove all non Alpha characters from.</param>
        /// <param name="includeAlso">A string treated like a char array, if any individual characters exist in
        /// the base string then those characters will be allowed through.  This will allow for exceptions with
        /// punctuation, white space, etc.</param>
        string RemoveNonAlpha(string buf, string includeAlso = "");

        /// <summary>
        /// Trims whitespace off of the front and end of a string.
        /// </summary>
        /// <param name="buf"></param>
        string Trim(string buf);

        /// <summary>
        /// Trims whitespace off of the front and end of a string.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="trimOff"></param>
        string Trim(string buf, string trimOff);

        /// <summary>
        /// Trims whitespace off of the start of a string.
        /// </summary>
        /// <param name="buf"></param>
        string TrimStart(string buf);

        /// <summary>
        /// Trims whitespace off of the start of a string.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="trimOff"></param>
        string TrimStart(string buf, string trimOff);

        /// <summary>
        /// Trims whitespace off the end of a string.
        /// </summary>
        /// <param name="buf"></param>
        string TrimEnd(string buf);

        /// <summary>
        /// Trims whitespace off the end of a string.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="trimOff"></param>
        string TrimEnd(string buf, string trimOff);

        /// <summary>
        /// Splits a string into a string array using a specified delimiter.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="delimiter"></param>
        string[] Split(string buf, string delimiter);

        /// <summary>
        /// Searches an array for whether a specified value exists within it.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="searchValue"></param>
        bool ArrayContains(string[] array, string searchValue);

        /// <summary>
        /// Removes all ANSI control sequences.
        /// </summary>
        /// <param name="str"></param>
        string RemoveAnsiCodes(string str);

        /// <summary>
        /// Removes all ANSI control sequences.
        /// </summary>
        /// <param name="array"></param>
        string[] RemoveAnsiCodes(string[] array);

        /// <summary>
        /// Removes empty elements from an array.
        /// </summary>
        /// <param name="array"></param>
        string[] RemoveElementsEmpty(string[] array);

        /// <summary>
        /// Removes elements from an array starting with a specified string.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="str"></param>
        string[] RemoveElementsStartsWith(string[] array, string str);

        /// <summary>
        /// Removes elements from an array ending with a specified string.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="str"></param>
        string[] RemoveElementsEndingWith(string[] array, string str);

        /// <summary>
        /// Removes elements from an array containing a specified string.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="str"></param>
        string[] RemoveElementsContains(string[] array, string str);

        /// <summary>
        /// Adds an item to a list.  Duplicate items are acceptable.
        /// </summary>
        /// <param name="sourceList"></param>
        /// <param name="value"></param>
        /// <param name="delimiter"></param>
        string ListAdd(string sourceList, string value, char delimiter = '|');

        /// <summary>
        /// Adds an item to a list at the start.
        /// </summary>
        /// <param name="sourceList"></param>
        /// <param name="value"></param>
        /// <param name="delimiter"></param>
        string ListAddStart(string sourceList, string value, char delimiter = '|');

        /// <summary>
        /// Adds an item to a list only if it doesn't exist.
        /// </summary>
        /// <param name="sourceList"></param>
        /// <param name="value"></param>
        /// <param name="delimiter"></param>
        string ListAddIfNotExist(string sourceList, string value, char delimiter = '|');

        /// <summary>
        /// Removes an item from a list.
        /// </summary>
        /// <param name="sourceList"></param>
        /// <param name="value"></param>
        /// <param name="delimiter"></param>
        string ListRemove(string sourceList, string value, char delimiter = '|');

        /// <summary>
        /// Removes 1 to n items from the end of a list.
        /// </summary>
        /// <param name="sourceList"></param>
        /// <param name="items"></param>
        /// <param name="delimiter"></param>
        string ListRemove(string sourceList, int items, char delimiter = '|');

        /// <summary>
        /// If an item exists in a list.
        /// </summary>
        /// <param name="sourceList"></param>
        /// <param name="value"></param>
        /// <param name="delimiter"></param>
        bool ListExists(string sourceList, string value, char delimiter = '|');

        /// <summary>
        /// Returns a new string with all occurrences of a string replaced with another string.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="searchValue"></param>
        /// <param name="replaceValue"></param>
        string Replace(string buf, string searchValue, string replaceValue);

        /// <summary>
        /// Enables all aliases and triggers in a group.
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns>Returns true if the group was found, false if it was not.</returns>
        bool EnableGroup(string groupName);

        /// <summary>
        /// Disables all aliases and triggers in a group.
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns>Returns true if the group was found, false if it was not.</returns>
        bool DisableGroup(string groupName);

        /// <summary>
        /// Adds a scheduled task (command or Lua) to be executed after a designated time.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="isLua"></param>
        /// <param name="seconds"></param>
        void AddScheduledTask(string command, bool isLua, int seconds);

        /// <summary>
        /// Adds a batch task (command or Lua) to be executed in order when the batch is run.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="isLua"></param>
        void AddBatchTask(string command, bool isLua);

        /// <summary>
        /// Starts the current batch processing.
        /// </summary>
        /// <param name="secondsInBetweenCommands"></param>
        void StartBatch(int secondsInBetweenCommands);

        /// <summary>
        /// Clears all tasks from the scheduled tasks queue.
        /// </summary>
        void ClearTasks();

        /// <summary>
        /// Formats a number as string with commas and no decimal places.
        /// </summary>
        /// <param name="value"></param>
        string FormatNumber(string value);

        /// <summary>
        /// Formats a number as string with commas with the specified number of decimal places.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="decimalPlaces"></param>
        string FormatNumber(string value, int decimalPlaces);

        /// <summary>
        /// Returns the last non-empty line in the game terminal.
        /// </summary>
        string LastNonEmptyLine();

        /// <summary>
        /// Returns a string array of the requested number of last lines from the game terminal.
        /// </summary>
        /// <param name="numberToTake"></param>
        string[] LastLines(int numberToTake);

        /// <summary>
        /// Returns a string array of the requested number of last lines from the game terminal.
        /// </summary>
        /// <param name="numberToTake"></param>
        /// <param name="reverseOrder">Whether the order of the array should be reversed.  True will return oldest line to newest, False will be newest to oldest.</param>
        string[] LastLines(int numberToTake, bool reverseOrder);

        /// <summary>
        /// Returns the last lines oldest to newest where the start line contains the search pattern.
        /// </summary>
        /// <param name="startLineContains"></param>
        string[] LastLinesBetweenContains(string startLineContains);

        /// <summary>
        /// Returns the last lines oldest to newest where the start line contains the search pattern and
        /// the end line contains it's search pattern.  Both patterns must be found or the list will be
        /// empty.
        /// </summary>
        /// <param name="startLineContains"></param>
        /// <param name="endLineContains"></param>
        string[] LastLinesBetweenContains(string startLineContains, string endLineContains);

        /// <summary>
        /// Returns the last lines oldest to newest where the start line contains the search pattern.
        /// </summary>
        /// <param name="startLineStartsWith"></param>
        string[] LastLinesBetweenStartsWith(string startLineStartsWith);

        /// <summary>
        /// Returns the last lines oldest to newest where the start line contains the search pattern and
        /// the end line contains it's search pattern.  Both patterns must be found or the list will be
        /// empty.
        /// </summary>
        /// <param name="startLineStartsWith"></param>
        /// <param name="endLineStartsWith"></param>
        string[] LastLinesBetweenStartsWith(string startLineStartsWith, string endLineStartsWith);

        /// <summary>
        /// Returns whether the string is a number.
        /// </summary>
        /// <param name="buf"></param>
        bool IsNumber(string buf);

        /// <summary>
        /// If the number is even.
        /// </summary>
        /// <param name="value"></param>
        bool IsEven(int value);

        /// <summary>
        /// If the number is odd.
        /// </summary>
        /// <param name="value"></param>
        bool IsOdd(int value);

        /// <summary>
        /// If the number is of the specified interval.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="interval"></param>
        bool IsInterval(int value, int interval);

        /// <summary>
        /// Returns the value if it falls in the range of the max and min.  Otherwise it returns
        /// the upper or lower boundary depending on which one the value passed.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        int Clamp(int value, int min, int max);

        /// <summary>
        /// Deletes the specified number of characters off the start of the string.  If the length
        /// is greater than the length of the string an empty string is returned.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="length"></param>
        string DeleteLeft(string buf, int length);

        /// <summary>
        /// Deletes the specified number of characters off the end of the string.  If the length
        /// is greater than the length of the string an empty string is returned.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="length"></param>
        string DeleteRight(string buf, int length);

        /// <summary>
        /// Returns the first word in the specified string.
        /// </summary>
        /// <param name="buf"></param>
        string FirstWord(string buf);

        /// <summary>
        /// Returns the second word in the specified string.
        /// </summary>
        /// <param name="buf"></param>
        string SecondWord(string buf);

        /// <summary>
        /// Returns the third word in the specified string.
        /// </summary>
        /// <param name="buf"></param>
        string ThirdWord(string buf);

        /// <summary>
        /// Returns the word by index from the provided string as delimited by spaces.  The delimiter
        /// can also be provided to specify a different split character.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="wordNumber"></param>
        /// <param name="delimiter"></param>
        string ParseWord(string buf, int wordNumber, string delimiter = " ");

        /// <summary>
        /// Returns a string with the specified word removed by index.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="wordIndex"></param>
        string RemoveWord(string buf, int wordIndex);

        /// <summary>
        /// Returns the string between the start marker and the end marker.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="beginMarker"></param>
        /// <param name="endMarker"></param>
        string Between(string buf, string beginMarker, string endMarker);

        /// <summary>
        /// Converts a string to Base64.
        /// </summary>
        /// <param name="buf"></param>
        string ToBase64(string buf);

        /// <summary>
        /// Converts a Base64 string back to it's original state.
        /// </summary>
        /// <param name="buf"></param>
        string FromBase64(string buf);

        /// <summary>
        /// HTML Encodes a string.
        /// </summary>
        /// <param name="buf"></param>
        string HtmlEncode(string buf);

        /// <summary>
        /// HTML decodes a string.
        /// </summary>
        /// <param name="buf"></param>
        string HtmlDecode(string buf);

        /// <summary>
        /// URL Encodes a string.
        /// </summary>
        /// <param name="buf"></param>
        string UrlEncode(string buf);

        /// <summary>
        /// URL Decodes a string.
        /// </summary>
        /// <param name="buf"></param>
        string UrlDecode(string buf);

        /// <summary>
        /// Returns the word count in the specified string.
        /// </summary>
        /// <param name="buf"></param>
        int WordCount(string buf);

        /// <summary>
        /// Returns a string that right aligns the instance by padding characters onto the left
        /// until the total width is attained.  If the total width is less than the provided string
        /// the provided string is returned.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="totalWidth"></param>
        string PadLeft(string buf, int totalWidth);

        /// <summary>
        /// Returns a string that left aligns the instance by padding characters onto the the right
        /// until the total width is attained.  If the total width is less than the provided string
        /// the provided string is returned.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="totalWidth"></param>
        string PadRight(string buf, int totalWidth);

        /// <summary>
        /// Returns the MD5 hash for the given string.
        /// </summary>
        /// <param name="value"></param>
        string MD5(string value);

        /// <summary>
        /// Returns the SHA256 hash for the given string.
        /// </summary>
        /// <param name="value"></param>
        string SHA256(string value);

        /// <summary>
        /// Returns the SHA512 hash for the given string.
        /// </summary>
        /// <param name="value"></param>
        string SHA512(string value);

        /// <summary>
        /// Removes all lines from a string that start with the specified text.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="searchText"></param>
        /// <remarks>Either parameter being null returns either the text if it's not null or a blank string if it was null.</remarks>
        string RemoveLinesStartingWith(string text, string searchText);

        /// <summary>
        /// Removes all lines from a string that end with the specified text.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="searchText"></param>
        /// <remarks>Either parameter being null returns either the text if it's not null or a blank string if it was null.</remarks>
        string RemoveLinesEndingWith(string text, string searchText);

        /// <summary>
        /// If a string starts with another string.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="searchText"></param>
        bool StartsWith(string text, string searchText);

        /// <summary>
        /// If a string ends with another string.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="searchText"></param>
        bool EndsWith(string text, string searchText);

        /// <summary>
        /// The number of Lua scripts that are actively running.
        /// </summary>
        int LuaScriptsActive();

        /// <summary>
        /// The current location of the profile save directory.
        /// </summary>
        string ProfileDirectory();

        /// <summary>
        /// Where the avalon setting file is stored that among other things has where the profile
        /// save directory is (in case that is in Dropbox, OneDrive, etc.).
        /// </summary>
        string AppDataDirectory();

        /// <summary>
        /// Adds a SQL command to the SqlTasks queue.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        void DbExecute(string sql, params string[] parameters);

        /// <summary>
        /// Executes a SQL command immediately outside of a transaction.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        void DbExecuteImmediate(string sql, params string[] parameters);

        /// <summary>
        /// Selects one value from the database.  If an error occurs it is written to the terminal.
        /// and an empty string is returned.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        object DbSelectValue(string sql, params string[] parameters);

        /// <summary>
        /// Selects a record set that can be iterated over in Lua as a table.  If an error occurs an it is
        /// written to the terminal and an empty result is returned.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        IEnumerable<Dictionary<string, object>> DbSelect(string sql, params string[] parameters);

        /// <summary>
        /// Forces all pending database operations to be committed.
        /// </summary>
        void DbFlush();

        /// <summary>
        /// Downloads a string from a URL using the GET method.
        /// </summary>
        /// <param name="url"></param>
        string HttpGet(string url);

        /// <summary>
        /// Downloads a string from a URL using the POST method.  Data is a formatted string
        /// posted as a form in the format: "Time = 12:00am temperature = 50";
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        string HttpPost(string url, string data);

        /// <summary>
        /// Sets the main status bar text with an optional icon lookup by name.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="iconName"></param>
        void SetText(string buf, string iconName);

        /// <summary>
        /// Sets the text on a specified status bar an optional text value look for the icon.
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="target"></param>
        /// <param name="iconName"></param>
        void SetText(string buf, TextTarget target = TextTarget.StatusBarText, string iconName = "None");

        /// <summary>
        /// Replaces the last occurrence of a string with another string in the main game terminal.
        /// </summary>
        /// <param name="searchText"></param>
        /// <param name="replacementText"></param>
        void TerminalReplaceLastInstance(string searchText, string replacementText);

        /// <summary>
        /// Replaces all instances of one string with another string in the main game terminal.
        /// </summary>
        /// <param name="searchText"></param>
        /// <param name="replacementText"></param>
        void TerminalReplaceAll(string searchText, string replacementText);

        /// <summary>
        /// Removes a line from the game terminal.
        /// </summary>
        /// <param name="lineNumber"></param>
        void TerminalRemoveLine(int lineNumber);

        /// <summary>
        /// Scrolls the main game terminal to the last line.
        /// </summary>
        /// <param name="lineNumber"></param>
        void TerminalScrollToLastLine(int lineNumber);

        /// <summary>
        /// Adds or replaces a replacement trigger.  A replacement is first identified by ID if provided and falls
        /// back to pattern if not.
        /// </summary>
        /// <param name="replace"></param>
        /// <param name="replaceWith"></param>
        /// <param name="id"></param>
        /// <param name="temp"></param>
        void AddReplacementTrigger(string replace, string replaceWith, string id, bool temp = false);

        /// <summary>
        /// Deletes a replacement trigger by ID.
        /// </summary>
        /// <param name="id"></param>
        void RemoveReplacementTrigger(string id);
    }
}
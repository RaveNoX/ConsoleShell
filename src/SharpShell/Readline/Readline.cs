#region Usings

using System;
using System.Collections;
using System.IO;
using System.Text;

#endregion

namespace SharpShell.Readline
{
    internal class Readline
    {
        public Readline(ShellHistory history)
        {
            _ctrlCInterrupts = Path.DirectorySeparatorChar == '/';
            Console.TreatControlCAsInput = !_ctrlCInterrupts;

            _history = history;
        }

        #region Fields

        // Internal state.
        private bool _ctrlCInterrupts;

        // Line input buffer.
        private readonly char[] _buffer = new char[256];
        private readonly byte[] _widths = new byte[256];
        private int _posn, _length, _column, _lastColumn;
        private bool _overwrite;
        private int _historyPosn;
        private string _historySave;
        private string _yankedString;

        private static char[] _wordBreakChars = { ' ', '\n' };
        private readonly ShellHistory _history;
        private ReadlineState _state = ReadlineState.None;
        private readonly StringBuilder _lastWord = new StringBuilder();
        private int _savePosn;
        private int _tabCount = -1;
        private int _insertedCount;

        #endregion

        #region Events

        /// <summary>
        ///     Event that is emitted to allow for tab completion.
        /// </summary>
        /// <remarks>
        ///     If there are no attached handlers, then the Tab key will do
        ///     normal tabbing.
        /// </remarks>
        public event EventHandler<TabCompleteEventArgs> TabComplete;

        /// <summary>
        ///     Event that is emitted to inform of the Ctrl+C command.
        /// </summary>
        public event EventHandler Interrupt;

        /// <summary>
        ///     Event that is emmited to write prompt
        /// </summary>
        public event EventHandler WritePrompt;

        public event EventHandler<PrintAlternativesEventArgs> PrintAlternatives;

        #endregion

        #region Properties        

        /// <summary>
        ///     Gets or sets a flag that indicates if CTRL-D is an EOF indication
        ///     or the "delete character" key.
        /// </summary>
        /// <remarks>
        ///     The default is true (i.e. EOF).
        /// </remarks>
        public bool CtrlDIsEOF { get; set; } = true;

        /// <summary>
        ///     Gets or sets a flag that indicates if CTRL-Z is an EOF indication.
        /// </summary>
        /// <remarks>
        ///     The default is true on Windows system, false otherwise.
        /// </remarks>
        public bool CtrlZIsEOF { get; set; } = Path.DirectorySeparatorChar == '\\';

        /// <summary>
        ///     Gets or sets a flag that indicates if CTRL-C is an EOF indication.
        /// </summary>
        /// <remarks>
        ///     The default is true on Unix system, false otherwise.
        /// </remarks>
        public bool CtrlCInterrupts
        {
            get { return _ctrlCInterrupts; }
            set
            {
                Console.TreatControlCAsInput = !value;
                _ctrlCInterrupts = value;
            }
        }


        public string LineBuffer => new string(_buffer, 0, _length);

        public char[] WordBreakCharacters
        {
            get { return _wordBreakChars; }
            set
            {
                if (value == null || value.Length == 0)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _wordBreakChars = value;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Makes room for one more character in the input buffer.
        /// </summary>
        private void MakeRoom()
        {
            if (_length >= _buffer.Length)
            {
                var newBuffer = new char[_buffer.Length * 2];
                var newWidths = new byte[_buffer.Length * 2];
                Array.Copy(_buffer, 0, newBuffer, 0, _buffer.Length);
                Array.Copy(_widths, 0, newWidths, 0, _buffer.Length);
            }
        }

        /// <summary>
        ///     Repaint the line starting at the current character.
        /// </summary>
        /// <param name="step"></param>
        /// <param name="moveToEnd"></param>
        private void Repaint(bool step, bool moveToEnd)
        {
            var posn = _posn;
            var column = _column;
            int width;

            // Paint the characters in the line.
            while (posn < _length)
            {
                if (_buffer[posn] == '\t')
                {
                    width = 8 - (column % 8);
                    _widths[posn] = (byte)width;
                    while (width > 0)
                    {
                        Console.Write(' ');
                        --width;
                        ++column;
                    }
                }
                else if (_buffer[posn] < 0x20)
                {
                    Console.Write('^');
                    Console.Write((char)(_buffer[posn] + 0x40));
                    _widths[posn] = 2;
                    column += 2;
                }
                else if (_buffer[posn] == '\u007F')
                {
                    Console.Write('^');
                    Console.Write('?');
                    _widths[posn] = 2;
                    column += 2;
                }
                else
                {
                    Console.Write(_buffer[posn]);
                    _widths[posn] = 1;
                    ++column;
                }
                ++posn;
            }

            // Adjust the position of the last column.
            if (column > _lastColumn)
            {
                _lastColumn = column;
            }
            else if (column < _lastColumn)
            {
                // We need to clear some characters beyond this point.
                width = _lastColumn - column;
                _lastColumn = column;
                while (width > 0)
                {
                    Console.Write(' ');
                    --width;
                    ++column;
                }
            }

            // Backspace to the initial cursor position.
            if (moveToEnd)
            {
                width = column - _lastColumn;
                _posn = _length;
            }
            else if (step)
            {
                width = column - (_column + _widths[_posn]);
                _column += _widths[_posn];
                ++(_posn);
            }
            else
            {
                width = column - _column;
            }
            while (width > 0)
            {
                Console.Write('\u0008');
                --width;
            }
        }

        /// <summary>
        ///     Add a character to the input buffer.
        /// </summary>
        /// <param name="ch"></param>
        private void AddChar(char ch)
        {
            if (_overwrite && _posn < _length)
            {
                _buffer[_posn] = ch;
                Repaint(true, false);
            }
            else
            {
                MakeRoom();
                if (_posn < _length)
                {
                    Array.Copy(_buffer, _posn, _buffer, _posn + 1, _length - _posn);
                }
                _buffer[_posn] = ch;
                ++_length;
                Repaint(true, false);
            }

            if (Array.IndexOf(_wordBreakChars, ch) != -1)
            {
                CollectLastWord(ch);
            }
        }

        private void CollectLastWord(char ch)
        {
            var chars = new ArrayList();
            for (var i = _length - 1; i >= 0; i--)
            {
                var c = _buffer[i];
                if (ch != '\0' && c == ch)
                    break;
                if (Array.IndexOf(_wordBreakChars, c) != -1)
                    break;

                chars.Add(c);
            }
            chars.Reverse();
            _lastWord.Length = 0;
            _lastWord.Append((char[])chars.ToArray(typeof(char)));
        }

        // Go back a specific number of characters.
        private void GoBack(int num)
        {
            while (num > 0)
            {
                --_posn;
                var width = _widths[_posn];
                _column -= width;
                while (width > 0)
                {
                    Console.Write('\u0008');
                    --width;
                }
                --num;
            }
        }

        // Backspace one character.
        private void Backspace()
        {
            if (_posn > 0)
            {
                GoBack(1);
                Delete();
            }
        }

        // Delete the character under the cursor.

        // Delete a number of characters under the cursor.
        private void Delete(int num = 1)
        {
            Array.Copy(_buffer, _posn + num, _buffer, _posn, _length - _posn - num);
            _length -= num;
            Repaint(false, false);
            //TODO: check...
            var chars = new ArrayList();
            for (var i = _length - 1; i >= 0; i--)
            {
                var c = _buffer[i];
                if (Array.IndexOf(_wordBreakChars, c) != -1)
                    break;
                chars.Add(c);
            }
            chars.Reverse();
            _lastWord.Length = 0;
            _lastWord.Append((char[])chars.ToArray(typeof(char)));
        }

        private void ResetComplete(ReadlineState newState)
        {
            if (_state == ReadlineState.Completing)
            {
                _tabCount = -1;
                _savePosn = -1;
            }

            _state = newState;
        }

        // Tab across to the next stop, or perform tab completion.
        private void Tab()
        {
            if (TabComplete == null)
            {
                // Add the TAB character and repaint the line.
                AddChar('\t');
            }
            else
            {
                if (_state != ReadlineState.Completing)
                {
                    CollectLastWord('\0');
                    _state = ReadlineState.Completing;
                }

                // Perform tab completion and insert the results.
                var e = new TabCompleteEventArgs(_lastWord.ToString(), ++_tabCount);
                TabComplete(this, e);
                if (e.Insert != null)
                {
                    if (_tabCount > 0)
                    {
                        GoBack(_insertedCount);
                        Delete(_insertedCount);
                    }

                    _insertedCount = e.Insert.Length;
                    _savePosn = _posn;
                    // Insert the value that we found.
                    var saveOverwrite = _overwrite;
                    _overwrite = false;
                    _savePosn = e.Insert.Length;

                    _state = ReadlineState.Completing;
                    foreach (var ch in e.Insert)
                    {
                        AddChar(ch);
                    }
                    _overwrite = saveOverwrite;
                }
                else if (e.Alternatives != null && e.Alternatives.Length > 0)
                {
                    // Print the alternatives for the user.
                    _savePosn = _posn;
                    EndLine();
                    PrintAlternatives?.Invoke(this, new PrintAlternativesEventArgs(e.Alternatives));
                    WritePrompt?.Invoke(this, EventArgs.Empty);

                    _posn = _savePosn;
                    _state = ReadlineState.Completing;
                    Redraw();
                }
                else
                {
                    if (e.Error)
                    {
                        ResetComplete(ReadlineState.MoreInput);
                    }

                    // No alternatives, or alternatives not supplied yet.
                    Console.Beep();
                }
            }
        }

        // End the current line.
        private void EndLine()
        {
            // Repaint the line and move to the end.
            Repaint(false, true);

            // Output the line terminator to the terminal.
            Console.Write(Environment.NewLine);
        }

        // Move left one character.
        private void MoveLeft()
        {
            if (_posn > 0)
            {
                GoBack(1);
            }
        }

        // Move right one character.
        private void MoveRight()
        {
            if (_posn < _length)
            {
                Repaint(true, false);
            }
        }

        // Set the current buffer contents to a historical string.
        private void SetCurrent(string line)
        {
            if (line == null)
            {
                line = string.Empty;
            }
            Clear();
            foreach (var ch in line)
            {
                AddChar(ch);
            }
        }

        // Move up one line in the history.
        private void MoveUp()
        {
            if (_history == null)
            {
                Console.Beep();
                return;
            }

            if (_historyPosn == -1)
            {
                if (_history.Count > 0)
                {
                    _historySave = new string(_buffer, 0, _length);
                    _historyPosn = 0;
                    SetCurrent(_history.GetItemAt(_historyPosn));
                }
            }
            else if ((_historyPosn + 1) < _history.Count)
            {
                ++_historyPosn;
                SetCurrent(_history.GetItemAt(_historyPosn));
            }
            else
            {
                Console.Beep();
            }
        }

        // Move down one line in the history.
        private void MoveDown()
        {
            if (_history == null)
            {
                Console.Beep();
                return;
            }

            if (_historyPosn == 0)
            {
                _historyPosn = -1;
                SetCurrent(_historySave);
            }
            else if (_historyPosn > 0)
            {
                --_historyPosn;
                SetCurrent(_history.GetItemAt(_historyPosn));
            }
            else
            {
                Console.Beep();
            }
        }

        // Move to the beginning of the current line.
        private void MoveHome()
        {
            GoBack(_posn);
        }

        // Move to the end of the current line.
        private void MoveEnd()
        {
            Repaint(false, true);
        }

        // Clear the entire line.
        private void Clear()
        {
            GoBack(_posn);
            _length = 0;
            Repaint(false, false);
        }

        // Cancel the current line and start afresh with a new prompt.
        private void CancelLine()
        {
            EndLine();
            WritePrompt?.Invoke(this, EventArgs.Empty);
            _posn = 0;
            _length = 0;
            _column = 0;
            _lastColumn = 0;
            _historyPosn = -1;
        }

        // Redraw the current line.
        private void Redraw()
        {
            var str = new string(_buffer, 0, _length);
            var savePosn = _posn;
            _posn = 0;
            _length = 0;
            _column = 0;
            _lastColumn = 0;
            foreach (var ch in str)
            {
                AddChar(ch);
            }
            GoBack(_length - savePosn);
        }

        // Erase all characters until the start of the current line.
        private void EraseToStart()
        {
            if (_posn > 0)
            {
                var savePosn = _posn;
                _yankedString = new string(_buffer, 0, _posn);
                GoBack(savePosn);
                Delete(savePosn);
            }
        }

        // Erase all characters until the end of the current line.
        private void EraseToEnd()
        {
            _yankedString = new string(_buffer, _posn, _length - _posn);
            _length = _posn;
            Repaint(false, false);
            _lastWord.Length = 0;
        }

        // Erase the previous word on the current line (delimited by whitespace).
        private void EraseWord()
        {
            var temp = _posn;
            while (temp > 0 && char.IsWhiteSpace(_buffer[temp - 1]))
            {
                --temp;
            }
            while (temp > 0 && !char.IsWhiteSpace(_buffer[temp - 1]))
            {
                --temp;
            }
            if (temp < _posn)
            {
                temp = _posn - temp;
                GoBack(temp);
                _yankedString = new string(_buffer, _posn, temp);
                Delete(temp);
            }

            if (_state != ReadlineState.Completing)
                _lastWord.Length = 0;
        }

        // Determine if a character is a "word character" (letter or digit).
        private bool IsWordCharacter(char ch)
        {
            return char.IsLetterOrDigit(ch);
        }

        // Erase to the end of the current word.
        private void EraseToEndWord()
        {
            var temp = _posn;
            while (temp < _length && !IsWordCharacter(_buffer[temp]))
            {
                ++temp;
            }
            while (temp < _length && IsWordCharacter(_buffer[temp]))
            {
                ++temp;
            }
            if (temp > _posn)
            {
                temp -= _posn;
                _yankedString = new string(_buffer, _posn, temp);
                Delete(temp);
            }
        }

        // Erase to the start of the current word.
        private void EraseToStartWord()
        {
            var temp = _posn;
            while (temp > 0 && !IsWordCharacter(_buffer[temp - 1]))
            {
                --temp;
            }
            while (temp > 0 && IsWordCharacter(_buffer[temp - 1]))
            {
                --temp;
            }
            if (temp < _posn)
            {
                temp = _posn - temp;
                GoBack(temp);
                _yankedString = new string(_buffer, _posn, temp);
                Delete(temp);
            }
        }

        // Move forward one word in the input line.
        private void MoveForwardWord()
        {
            while (_posn < _length && !IsWordCharacter(_buffer[_posn]))
            {
                MoveRight();
            }
            while (_posn < _length && IsWordCharacter(_buffer[_posn]))
            {
                MoveRight();
            }
        }

        // Move backward one word in the input line.
        private void MoveBackwardWord()
        {
            while (_posn > 0 && !IsWordCharacter(_buffer[_posn - 1]))
            {
                MoveLeft();
            }
            while (_posn > 0 && IsWordCharacter(_buffer[_posn - 1]))
            {
                MoveLeft();
            }
        }

        #endregion

        #region Public Methods

        // Read the next line of input using line editing.  Returns "null"
        // if an EOF indication is encountered in the input.
        public string ReadLine()
        {
            // Output the prompt.
            WritePrompt?.Invoke(this, EventArgs.Empty);

            // Enter the main character input loop.
            _posn = 0;
            _length = 0;
            _column = 0;
            _lastColumn = 0;
            _overwrite = false;
            _historyPosn = -1;
            var ctrlv = false;
            _state = ReadlineState.MoreInput;
            do
            {
                var key = ConsoleExtensions.ReadKey(true);
                var ch = key.KeyChar;
                if (ctrlv)
                {
                    ctrlv = false;
                    if ((ch >= 0x0001 && ch <= 0x001F) || ch == 0x007F)
                    {
                        // Insert a control character into the buffer.
                        AddChar(ch);
                        continue;
                    }
                }
                if (ch != '\0')
                {
                    switch (ch)
                    {
                        case '\u0001':
                            {
                                // CTRL-A: move to the home position.
                                MoveHome();
                            }
                            break;

                        case '\u0002':
                            {
                                // CTRL-B: go back one character.
                                MoveLeft();
                            }
                            break;

                        case '\u0003':
                            {
                                // CTRL-C encountered in "raw" mode.
                                if (_ctrlCInterrupts)
                                {
                                    EndLine();
                                    Interrupt?.Invoke(null, EventArgs.Empty);
                                    return null;
                                }
                                CancelLine();
                                _lastWord.Length = 0;
                            }
                            break;

                        case '\u0004':
                            {
                                // CTRL-D: EOF or delete the current character.
                                if (CtrlDIsEOF)
                                {
                                    _lastWord.Length = 0;
                                    // Signal an EOF if the buffer is empty.
                                    if (_length == 0)
                                    {
                                        EndLine();
                                        return null;
                                    }
                                }
                                else
                                {
                                    Delete();
                                    ResetComplete(ReadlineState.MoreInput);
                                }
                            }
                            break;

                        case '\u0005':
                            {
                                // CTRL-E: move to the end position.
                                MoveEnd();
                            }
                            break;

                        case '\u0006':
                            {
                                // CTRL-F: go forward one character.
                                MoveRight();
                            }
                            break;

                        case '\u0007':
                            {
                                // CTRL-G: ring the terminal bell.
                                Console.Beep();
                            }
                            break;

                        case '\u0008':
                        case '\u007F':
                            {
                                if (key.Key == ConsoleKey.Delete)
                                {
                                    // Delete the character under the cursor.
                                    Delete();
                                }
                                else
                                {
                                    // Delete the character before the cursor.
                                    Backspace();
                                }
                                ResetComplete(ReadlineState.MoreInput);
                            }
                            break;

                        case '\u0009':
                            {
                                // Process a tab.
                                Tab();
                            }
                            break;

                        case '\u000A':
                        case '\u000D':
                            {
                                // Line termination.
                                EndLine();
                                ResetComplete(ReadlineState.Done);
                                _lastWord.Length = 0;
                            }
                            break;

                        case '\u000B':
                            {
                                // CTRL-K: erase until the end of the line.
                                EraseToEnd();
                            }
                            break;

                        case '\u000C':
                            {
                                // CTRL-L: clear screen and redraw.
                                Console.Clear();
                                WritePrompt?.Invoke(this, EventArgs.Empty);
                                Redraw();
                            }
                            break;

                        case '\u000E':
                            {
                                // CTRL-N: move down in the history.
                                MoveDown();
                            }
                            break;

                        case '\u0010':
                            {
                                // CTRL-P: move up in the history.
                                MoveUp();
                            }
                            break;

                        case '\u0015':
                            {
                                // CTRL-U: erase to the start of the line.
                                EraseToStart();
                                ResetComplete(ReadlineState.None);
                            }
                            break;

                        case '\u0016':
                            {
                                // CTRL-V: prefix a control character.
                                ctrlv = true;
                            }
                            break;

                        case '\u0017':
                            {
                                // CTRL-W: erase the previous word.
                                EraseWord();
                                ResetComplete(ReadlineState.MoreInput);
                            }
                            break;

                        case '\u0019':
                            {
                                // CTRL-Y: yank the last erased string.
                                if (_yankedString != null)
                                {
                                    foreach (var ch2 in _yankedString)
                                    {
                                        AddChar(ch2);
                                    }
                                }
                            }
                            break;

                        case '\u001A':
                            {
                                // CTRL-Z: Windows end of file indication.
                                if (CtrlZIsEOF && _length == 0)
                                {
                                    EndLine();
                                    return null;
                                }
                            }
                            break;

                        case '\u001B':
                            {
                                // Escape is "clear line".
                                Clear();
                                ResetComplete(ReadlineState.MoreInput);
                            }
                            break;

                        default:
                            {
                                if (ch >= ' ')
                                {
                                    // Ordinary character.
                                    AddChar(ch);
                                    ResetComplete(ReadlineState.MoreInput);
                                }
                            }
                            break;
                    }
                }
                else if (key.Modifiers == 0)
                {
                    switch (key.Key)
                    {
                        case ConsoleKey.Backspace:
                            {
                                // Delete the character before the cursor.
                                Backspace();
                                ResetComplete(ReadlineState.MoreInput);
                            }
                            break;

                        case ConsoleKey.Delete:
                            {
                                // Delete the character under the cursor.
                                Delete();
                                ResetComplete(ReadlineState.MoreInput);
                            }
                            break;

                        case ConsoleKey.Enter:
                            {
                                // Line termination.
                                EndLine();
                                ResetComplete(ReadlineState.Done);
                            }
                            break;

                        case ConsoleKey.Escape:
                            {
                                // Clear the current line.
                                Clear();
                                ResetComplete(ReadlineState.None);
                            }
                            break;

                        case ConsoleKey.Tab:
                            {
                                // Process a tab.
                                Tab();
                            }
                            break;

                        case ConsoleKey.LeftArrow:
                            {
                                // Move left one character.
                                MoveLeft();
                            }
                            break;

                        case ConsoleKey.RightArrow:
                            {
                                // Move right one character.
                                MoveRight();
                            }
                            break;

                        case ConsoleKey.UpArrow:
                            {
                                // Move up one line in the history.
                                MoveUp();
                            }
                            break;

                        case ConsoleKey.DownArrow:
                            {
                                // Move down one line in the history.
                                MoveDown();
                            }
                            break;

                        case ConsoleKey.Home:
                            {
                                // Move to the beginning of the line.
                                MoveHome();
                            }
                            break;

                        case ConsoleKey.End:
                            {
                                // Move to the end of the line.
                                MoveEnd();
                            }
                            break;

                        case ConsoleKey.Insert:
                            {
                                // Toggle insert/overwrite mode.
                                _overwrite = !_overwrite;
                            }
                            break;
                    }
                }
                else if ((key.Modifiers & ConsoleModifiers.Alt) != 0)
                {
                    switch (key.Key)
                    {
                        case ConsoleKey.F:
                            {
                                // ALT-F: move forward a word.
                                MoveForwardWord();
                            }
                            break;

                        case ConsoleKey.B:
                            {
                                // ALT-B: move backward a word.
                                MoveBackwardWord();
                            }
                            break;

                        case ConsoleKey.D:
                            {
                                // ALT-D: erase until the end of the word.
                                EraseToEndWord();
                            }
                            break;

                        case ConsoleKey.Backspace:
                        case ConsoleKey.Delete:
                            {
                                // ALT-DEL: erase until the start of the word.
                                EraseToStartWord();
                            }
                            break;
                    }
                }
            } while (_state != ReadlineState.Done);
            return new string(_buffer, 0, _length);
        }

        public string ReadPassword()
        {
            // Output the prompt.
            WritePrompt?.Invoke(this, EventArgs.Empty);

            var pass = new Stack();

            for (var consKeyInfo = Console.ReadKey(true);
                consKeyInfo.Key != ConsoleKey.Enter;
                consKeyInfo = Console.ReadKey(true))
            {
                if (consKeyInfo.Key == ConsoleKey.Backspace)
                {
                    try
                    {
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        Console.Write(" ");
                        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        pass.Pop();
                    }
                    catch (InvalidOperationException)
                    {
                        Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop);
                    }
                }
                else
                {
                    Console.Write("*");
                    pass.Push(consKeyInfo.KeyChar.ToString());
                }
            }
            var chars = pass.ToArray();
            var password = new string[chars.Length];
            Array.Copy(chars, password, chars.Length);
            Array.Reverse(password);
            return string.Join(string.Empty, password);
        }

        #endregion
    }
}
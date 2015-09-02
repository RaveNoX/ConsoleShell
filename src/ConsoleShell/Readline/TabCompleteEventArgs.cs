#region Usings

using System;

#endregion

namespace ConsoleShell.Readline
{
    internal class TabCompleteEventArgs : EventArgs
    {
        #region Fields

        // Internal state.
        private string _insert;

        #endregion

        #region ctor

        internal TabCompleteEventArgs(string text, int state)
        {
            Text = text;
            _insert = null;
            Alternatives = null;
            State = state;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Get the text before the last space to the current position.
        /// </summary>
        public string Text { get; }

        /// <summary>
        ///     Get or set the extra string to be inserted into the line.
        /// </summary>
        public string Insert
        {
            get { return _insert; }
            set
            {
                if (value != null)
                    _insert = value;
            }
        }

        public int State { get; }

        /// <summary>
        ///     Gets or sets the text that will be added to the current position
        ///     of the command line.
        /// </summary>
        public string Output
        {
            get { return (_insert == null ? Text : Text + _insert); }
            set
            {
                if (value == null)
                {
                    _insert = value;
                }
                else
                {
                    if (value.Length < Text.Length)
                    {
                        return;
                    }

                    var s = value.Substring(0, Text.Length);
                    if (string.Compare(Text, s, StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        throw new ArgumentException();
                    }
                    _insert = value.Substring(Text.Length);
                }
            }
        }

        /// <summary>
        ///     Get or set the list of strings to be displayed as alternatives.
        /// </summary>
        public string[] Alternatives { get; set; }

        public bool Error { get; set; }

        #endregion
    }
}
#region Usings

using System;

#endregion

namespace SharpShell.Readline
{
    internal static class ConsoleExtensions
    {
        /// <summary>
        ///     Emitted when the console window size changes.
        /// </summary>
        public static event EventHandler SizeChanged;

        /// <summary>
        ///     Emitted when the program is resumed after a suspend.
        /// </summary>
        public static event EventHandler Resumed;

        /// <summary>
        ///     Read a key while processing window resizes and process resumption.
        /// </summary>
        /// <returns>
        ///     <see cref="ConsoleKeyInfo" />
        /// </returns>
        public static ConsoleKeyInfo ReadKey()
        {
            return ReadKey(false);
        }

        /// <summary>
        ///     Read a key while processing window resizes and process resumption.
        /// </summary>
        /// <param name="intercept">If true, pressed key will not be displayed in console</param>
        /// <returns>
        ///     <see cref="ConsoleKeyInfo" />
        /// </returns>
        public static ConsoleKeyInfo ReadKey(bool intercept)
        {
            var key = Console.ReadKey(intercept);
            switch (key.Key)
            {
                case (ConsoleKey) 0x1200:
                    // "SizeChanged" key indication.
                    SizeChanged?.Invoke(null, EventArgs.Empty);
                    break;
                case (ConsoleKey) 0x1201:
                    // "Resumed" key indication.
                    Resumed?.Invoke(null, EventArgs.Empty);
                    break;
            }
            return key;
        }
    }
}
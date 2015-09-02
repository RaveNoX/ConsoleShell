#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

#endregion

namespace ConsoleShell
{
    public class ShellHistory : IEnumerable<string>
    {
        private readonly Queue<string> _history = new Queue<string>();
        private readonly object _lock = new object();
        private int _maxItems;

        /// <summary>
        ///     Gets or sets the maximum history list size.
        /// </summary>
        /// <remarks>
        ///     If this the history should have no limit, use 0.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     If the given value is smaller than 0.
        /// </exception>
        public int MaximumItems
        {
            get { return _maxItems; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                lock (_lock)
                {
                    _maxItems = value;
                    FitHistoryToSize();
                }
            }
        }

        /// <summary>
        ///     Gets the number of items currently in the history.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _history.Count;
                }
            }
        }

        public IEnumerator<string> GetEnumerator()
        {
            string[] items;

            lock (_lock)
            {
                items = _history.ToArray();
            }

            return items.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void FitHistoryToSize()
        {
            if (_maxItems == 0) return;

            while (_history.Count > _maxItems)
            {
                _history.Dequeue();
            }
        }

        /// <summary>
        ///     Add a line of input to the history.
        ///     If line is empty or white-space it will be skipped.
        /// </summary>
        /// <param name="line">The line string to add to the history.</param>
        public void Add(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            lock (_lock)
            {
                _history.Enqueue(line);
                FitHistoryToSize();
            }
        }

        /// <summary>
        ///     Adds a line of input to the history.
        ///     If it is different from the most recent line that is present.
        ///     If line is empty or white-space it will be skipped.
        /// </summary>
        /// <param name="line">The line string to add to the history.</param>
        public void AddUnique(string line)
        {
            lock (_lock)
            {
                if (!_history.Any() || _history.Last() != line)
                {
                    Add(line);
                }
            }
        }

        /// <summary>
        ///     Clear the scroll-back history.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _history.Clear();
            }
        }

        /// <summary>
        /// Returs line at position
        /// </summary>
        /// <param name="index">index of line</param>
        /// <returns>Line at position or null</returns>
        public string GetItemAt(int index)
        {
            lock (_lock)
            {
                return _history.ElementAtOrDefault(_history.Count - 1 - index);
            }
        }

        /// <summary>
        ///     Loads a set of history lines from a <see cref="TextReader" />.
        /// </summary>
        /// <param name="reader"><see cref="TextReader" /> for read from</param>
        public void Load(TextReader reader)
        {
            lock (_lock)
            {
                string line;
                Clear();

                while ((line = reader.ReadLine()) != null)
                {
                    Add(line);
                }
            }
        }


        /// <summary>
        ///     Loads a set of history lines from a <see cref="Stream" />.
        /// </summary>
        /// <param name="stream"><see cref="Stream" /> for read from</param>
        public void Load(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                Load(reader);
            }
        }

        /// <summary>
        ///     Loads a set of history lines from a given file.
        /// </summary>
        /// <param name="file">The path to the file containing the history lines.</param>
        public void Load(string file)
        {            
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Load(stream);
            }
        }


        /// <summary>
        ///     Saves the history lines to <see cref="TextWriter" />
        /// </summary>
        /// <param name="writer"><see cref="TextWriter" /> where to store history lines</param>
        public void Save(TextWriter writer)
        {
            foreach (var line in this)
            {
                writer.WriteLine(line);
            }
            writer.Flush();
        }

        /// <summary>
        ///     Saves the history lines to <see cref="Stream" />
        /// </summary>
        /// <param name="stream"><see cref="Stream" /> where to store history lines</param>
        public void Save(Stream stream)
        {
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                Save(writer);
            }
        }

        /// <summary>
        ///     Saves the history lines to file.
        /// </summary>
        /// <param name="file">The path to the file where to store history lines</param>
        public void Save(string file)
        {
            using (var stream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Save(stream);
            }
        }
    }
}
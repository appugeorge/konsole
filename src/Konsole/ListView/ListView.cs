﻿using System;
using System.Collections.Generic;
using System.Linq;
using Konsole.Internal;
using static System.ConsoleColor;

namespace Konsole
{
    public class ListView<T> : ITheme
    {
        public int selectedItemIndex { get; set; }
        public Column[] Columns { get; set; }

        protected Func<T, string[]> _getRow;
        private int startRecord = 0;
        private readonly IConsole _console;
        protected Func<IEnumerable<T>> _getData;

        public ControlStatus Status { get; set; } = ControlStatus.Active;
        public StyleTheme Theme { get; set; } = StyleTheme.Default;
        public Style Style
        {
            get
            {
                return Theme.GetActive(Status);
            }
        }

        /// <summary>
        /// /// set this to a function that will override the default theme for a column
        /// if you need to customise the colour of a specific item. e.g. 
        /// highlighting a specific account that is ON_HOLD.
        /// </summary>
        /// <remarks>columnNo is unusually '1' based for these rules.</remarks>
        public Func<T, int, Colors> BusinessRuleColors = null;

        public ListView(IConsole console, Func<T[]> getData, Func<T, string[]> getRow, params Column[] columns)
        {
            _console = console;
            _getData = getData;
            _getRow = getRow;
            Columns = columns;
        }

        public void Refresh()
        {
            // can later add in overloads to the refesh to decide how much to refresh, e.g. just refresh the border(box) etc.
            // TODO: write concurrency test that proves that we need this lock here! Test must fail if I take it out !
            lock (Window._locker)
            {
                int cnt = Columns.Count();
                var columns = Columns.ResizeColumns(_console.WindowWidth);
                int width = _console.WindowWidth;
                var colLen = columns.Sum(c => c.resized);

                // certain that we can do better than
                // clearing each time. Make sure we always write the full width
                // then we can simply clear below where we write to, if we can 
                // tell if it was empty, then no need.

                _console.Clear();
                var items = _getData().ToArray();
                int cntRows = items.Length;
                PrintColumnHeadings(columns);
                int rowIndex = 0;
                foreach (var item in items)
                {
                    rowIndex++;
                    int i = 0;
                    bool lastRow = (rowIndex == cntRows) && (_console.CursorTop +1 == _console.WindowHeight);
                    var row = _getRow(item);
                    foreach (var columnText in row)
                    {
                        var column = columns[i].column;
                        int cwidth = columns[i].resized;
                        bool lastColumn = (++i == cnt);
                        var colors = getColors(item, i);
                        if (lastColumn)
                        {
                            if(lastRow)
                                _console.Write(colors, columnText.FixLeft(cwidth));
                            else
                                _console.WriteLine(colors, columnText.FixLeft(cwidth));                            
                        }
                        else
                        {
                            _console.Write(colors, columnText.FixLeft(cwidth));
                            // TODO: the bar needs to come from the parent frame
                            _console.Write("│");
                        }
                    }
                }
            }
        }

        private Colors getColors(T item, int column)
        {
            // allow users to override the colouring for a column and a row
            // based on a set of business rules
            var rules = BusinessRuleColors;
            if (rules != null)
            {
                var colors = rules(item, column);
                if (colors != null) return colors;
            }
            return Style.Body;
        }

        private void PrintColumnHeadings((Column column, int width)[] resized)
        {
            int i = 0;
            int len = resized.Length;
            var colors = Style?.Title ?? new Colors(Yellow, Black);
            foreach (var item in resized)
            {
                var col = item.column;
                bool last = (++i == len);
                if (last)
                {
                    _console.WriteLine(colors, col.Name.FixCenter(item.width));
                }
                else
                {
                    _console.Write(colors, col.Name.FixCenter(item.width));
                    _console.Write(colors, "│");
                }
            }

        }





    }
}

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using Renamer.Common;

//A filter is a function applied to a string
//The application will use ~24 filters

namespace Renamer.Models
{
    public enum FilterType
    {
        Clear,

        AddNumbering,
        NumberByDirectories,
        SwapOrder,

        AppendBefore,
        AppendAfter,
        AppendAtPosition,
        AppendFromTextFile,

        KeepNumeric,
        KeepAlphanumeric,
        RemoveInvalidCharacters,

        PreserveFromLeft,
        PreserveFromRight,
        TrimFromLeft,
        TrimFromRight,

        CapitalizeEachWord,
        UpperCase,
        LowerCase,
        SentenceCase,

        Regex,
        RegexReplace,
        ReplaceString,
        ReplaceCaseInsensitive,

        ParentDirectory,
        AddExtension,
        RemoveExtension       
    }

    public class Filter
    {
        public FilterType filterType;

        private int position;
        private string text1;
        private string text2;

        public string FirstArgument;
        public string SecondArgument;

        private List<string> lines; 

        #region constructors

        //()
        //(alt, [pos])
        //(text1)
        //(text1, pos)
        //(pos)
        //(text1, text2)

        public Filter(FilterType type, object x = null, object y = null)
        {
            filterType = type;

            FirstArgument = (x == null ? "" : x.ToString());
            SecondArgument = (y == null ? "" : y.ToString());

            if (filterType == FilterType.AppendBefore || filterType == FilterType.AppendAfter || 
                filterType == FilterType.Regex)
            {
                text1 = FirstArgument;
            }

            else if (filterType == FilterType.AppendAtPosition || filterType == FilterType.AppendFromTextFile)
            {
                text1 = FirstArgument;
                position = Convert.ToInt32(y);
            }

            else if (filterType == FilterType.AddNumbering || filterType==FilterType.NumberByDirectories || filterType == FilterType.SwapOrder ||
                     filterType == FilterType.PreserveFromLeft || filterType == FilterType.PreserveFromRight ||
                     filterType == FilterType.TrimFromLeft || filterType == FilterType.TrimFromRight ||
                     filterType == FilterType.ParentDirectory)
            {
                position = Convert.ToInt32(x);
            }

            else if (filterType == FilterType.RegexReplace || 
                     filterType == FilterType.ReplaceString || filterType == FilterType.ReplaceCaseInsensitive)
            {
                text1 = FirstArgument;
                text2 = SecondArgument;
            }

            //if (filterType == FilterType.AddNumbering || filterType == FilterType.SwapOrder)
            //{
                //Nothing to do
            //}

            if (filterType == FilterType.AppendFromTextFile)
            {
                //Read text file and store its lines
                lines = new List<string>();

                try
                {
                    using (var reader = new StreamReader(text1))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }
            }
        }

        #endregion

        public string GetFunctionName()
        {
            return filterType.ToString().SplitPascalCase();
        }

        //(base 0) index is only required for AddNumbering (and Swap Order) and AppendFromTextFile
        //max is the number of files, it's used to fill with zeros if FilterType is AddNumbering
        //original is only needed for AddExtension and ParentDirectory

        private FileName previousFileName = null;   //previously used file name
        private int fileCount = 0;                  //file count in the current directory
        
        //public string ApplyTo(string input, int index=0, int max=0, FileName fn=null)
        public string ApplyTo(FileName fn, int index = 0, int max = 0)
        {
            //the input will be the latest output in this case, the modified name of the current file
            string input = fn.Modified;

            switch (filterType)
            {
                case FilterType.Clear:
                    return input.Clear();

                case FilterType.AddNumbering:
                    var number = (index + 1).CompleteZeros(max);
                    return input.AppendAtPosition(number, position);

                case FilterType.NumberByDirectories:
                    if (previousFileName != null && previousFileName.ParentDirectory() != fn.ParentDirectory()) fileCount = 0;
                    
                    previousFileName = fn;  
                    fileCount++;                                     

                    return input.AppendAtPosition((fileCount).CompleteZeros(max), position);

                case FilterType.SwapOrder:
                    var order = index;
                    if (index%2 == 0) order += 2;
                    return input.AppendAtPosition(order.CompleteZeros(max), position);

                case FilterType.AppendBefore:
                    return input.AppendBefore(text1);

                case FilterType.AppendAfter:
                    return input.AppendAfter(text1);

                case FilterType.AppendFromTextFile:
                    if (index + 1 > lines.Count) return input;
                    return input.AppendAtPosition(lines[index], position);

                case FilterType.AppendAtPosition:
                    return input.AppendAtPosition(text1, position);

                case FilterType.KeepNumeric:
                    return input.ExtractNumeric();

                case FilterType.KeepAlphanumeric:
                    return input.ExtractAlphanumeric();

                case FilterType.RemoveInvalidCharacters:
                    return input.Clean();

                case FilterType.PreserveFromLeft:
                    return input.KeepLeft(position);

                case FilterType.PreserveFromRight:
                    return input.KeepRight(position);

                case FilterType.TrimFromLeft:
                    return input.TrimLeft(position);

                case FilterType.TrimFromRight:
                    return input.TrimRight(position);

                case FilterType.CapitalizeEachWord:
                    return input.CapitalizeEachWord();

                case FilterType.UpperCase:
                    return input.ToUpper();

                case FilterType.LowerCase:
                    return input.ToLower();

                case FilterType.SentenceCase:
                    return input.SentenceCase();

                case FilterType.Regex:
                    try
                    {
                        var match = new Regex(text1).Match(input);
                        if (match.Success) return match.Value;
                    }
                    catch (ArgumentException exception)
                    {
                        Console.WriteLine(exception.Message);
                    }
                    return string.Empty;

                case FilterType.RegexReplace:
                    try
                    {
                        return Regex.Replace(input, text1, text2);
                    }
                    catch (ArgumentException exception)
                    {
                        Console.WriteLine(exception.Message);
                    }
                    return string.Empty;

                case FilterType.ReplaceString:
                    if (text1 == "") return input;
                    return input.Replace(text1, text2);

                case FilterType.ReplaceCaseInsensitive:
                    return input.ReplaceInsensitive(text1, text2);

                case FilterType.AddExtension:
                    return input + fn.GetExtension();

                case FilterType.RemoveExtension:
                    return fn.GetModifiedNameWithoutExtension();

                case FilterType.ParentDirectory:
                    return input.AppendAtPosition(fn.ParentDirectory(), position);

                default:
                    return string.Empty;
            }
        }

    }
}

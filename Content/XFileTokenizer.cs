/*
 * XFileTokenizer.cs
 * Creates a tokenizer for DirectX files
 * Part of XNA Animation Component library, which is a library for animation
 * in XNA
 * 
 * Copyright (C) 2006 David Astle
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Text;
using System.IO;
#endregion


namespace Animation.Content
{
    /// <summary>
    /// Tokenizes a .X file and provides methods to parse those tokens.
    /// </summary>
    public class XFileTokenizer
    {
        #region Member Variables
        // Used to build each token so that we don't have to deal with the inefficiency of
        // string concatenation.
        private StringBuilder sb = new StringBuilder();
        // Designates the starting index of the stringbuilder for the current token
        // we are building.  This allows us to not clear the string builder every time
        // we start building a new token, and increases performance.
        private int index = 0;
        // The length of the current token.
        private int length = 0;
        // Stores the index of the current token when other classes are using this to read a
        // a file.
        private long tokenIndex = 0;
        private string[] tokens;
        #endregion

        #region Constructors
        public XFileTokenizer(string fileName)
        {
            string s = File.ReadAllText(fileName);
            tokens = TokensFromString(s);
        }
        #endregion

        #region Properties
        // Returns current token while ITERATING through the tokens
        public string CurrentToken
        { get { return tokens[tokenIndex - 1]; } }

        public string Peek
        { get { return tokens[tokenIndex]; } }

        // Returns current string while BUILDING the tokens
        private string CurrentString
        { get { return sb.ToString(index, length); } }

        // Are we at the end of the token stream while a client is iterating the tokens?
        public bool AtEnd
        { get { return tokenIndex >= tokens.Length - 1; } }

        public int Count
        { get { return tokens.Length; } }

        #endregion

        #region Methods
        // Adds a character to our current token.
        private void AddChar(int c)
        {
            sb.Append((char)c);
            length++;
        }

        // Tells the stringbuilder that we are starting a new token.
        private void ResetString()
        {
            index = index + length;
            // clear sb if it is getting too big; allows us to read long files
            if (index > int.MaxValue / 2)
            {
                sb.Remove(0, sb.Length);
                index = 0;
            }
            length = 0;
        }

        // When parsing a token fails, throws an error that says
        // what tokens surround the ill-parsed token
        private void Throw(Type type)
        {
            string error = "Failed to parse " + type.ToString() +
                " at token number " + tokenIndex.ToString() + "\nvalue: " +
                tokens[tokenIndex] + "\nSurrounding tokens: \n";
            long start = tokenIndex - 15;
            if (start < 0)
                start = 0;
            long end = tokenIndex + 15;
            if (end > Count - 1)
                end = Count - 1;
            for (long i = start; i <= end; i++)
                if (i == tokenIndex)
                    error += "*" + tokens[i];
                else
                    error += tokens[i];

            throw new Exception(error);
        }


        // The .X file format is  broad, and allows for custom data nodes to be created.  
        // We are only interested in the primary ones, so this function skips unneeded data
        // nodes
        public void SkipNode()
        {
            string next = NextToken();
            while (next != "}")
                if ((next = NextToken()) == "{")
                    SkipNode();

        }

        // This and functions like it parse the current token/tokens into the
        // appropriate data type
        public int NextInt()
        {
            int x = 0;
            try
            {
                x = int.Parse(tokens[tokenIndex++]);
            }
            catch
            {
                Throw(typeof(int));
            }

            tokenIndex++;
            return x;
        }

        public float NextFloat()
        {
            float x = 0;
            try
            {
                x = float.Parse(tokens[tokenIndex++]);
            }
            catch
            {
                Throw(typeof(float));
            }
            finally
            {
                tokenIndex++;
            }
            return x;
        }


        public string NextString()
        {
            string s = NextToken().Trim('"');
            SkipToken();
            return s;
        }


        public string NextToken()
        {
            string s = null;

            try
            {
                s = tokens[tokenIndex];
            }
            catch
            {
                throw new IndexOutOfRangeException("Tried to read token when there were " +
                    " no more tokens left.");
            }
            finally
            {
                tokenIndex++;
            }

            return s;
        }

        public Vector2 NextVector2()
        {
            try
            {
                Vector2 v = new Vector2(
                    float.Parse(tokens[tokenIndex]),
                    float.Parse(tokens[tokenIndex + 2]));
                return v;
            }
            catch
            {
                Throw(typeof(Vector2));
            }
            finally
            {
                tokenIndex += 5;
            }
            return Vector2.Zero;
        }

        public Vector3 NextVector3()
        {

            try
            {
                Vector3 v = new Vector3(
                    float.Parse(tokens[tokenIndex]),
                    float.Parse(tokens[tokenIndex + 2]),
                    float.Parse(tokens[tokenIndex + 4]));
                return v;
            }
            catch
            {
                Throw(typeof(Vector3));
            }
            finally
            {
                tokenIndex += 7;
            }
            return Vector3.Zero;
        }

        public Vector4 NextVector4()
        {
            tokenIndex += 9;
            try
            {
                Vector4 v = new Vector4(
                    float.Parse(tokens[tokenIndex - 9]),
                    float.Parse(tokens[tokenIndex - 7]),
                    float.Parse(tokens[tokenIndex - 5]),
                    float.Parse(tokens[tokenIndex - 3]));
                return v;
            }
            catch
            {
                Throw(typeof(Vector4));
            }
            return Vector4.Zero;
        }


        public Matrix NextMatrix()
        {

            try
            {
                Matrix m = new Matrix(
                    float.Parse(tokens[tokenIndex]), float.Parse(tokens[tokenIndex + 2]),
                    float.Parse(tokens[tokenIndex + 4]), float.Parse(tokens[tokenIndex + 6]),
                    float.Parse(tokens[tokenIndex + 8]), float.Parse(tokens[tokenIndex + 10]),
                    float.Parse(tokens[tokenIndex + 12]), float.Parse(tokens[tokenIndex + 14]),
                    float.Parse(tokens[tokenIndex + 16]), float.Parse(tokens[tokenIndex + 18]),
                    float.Parse(tokens[tokenIndex + 20]), float.Parse(tokens[tokenIndex + 22]),
                    float.Parse(tokens[tokenIndex + 24]), float.Parse(tokens[tokenIndex + 26]),
                    float.Parse(tokens[tokenIndex + 28]), float.Parse(tokens[tokenIndex + 30]));
                return m;
            }
            catch
            {
                Throw(typeof(Matrix));
            }
            finally
            {
                tokenIndex += 33;
            }
            return new Matrix();
        }




        public void SkipTokens(int numToSkip)
        { tokenIndex += numToSkip; }



        // All nodes in directx can have names, but we don't care about
        // the names of many nodes.
        public XFileTokenizer SkipName()
        {
            ReadName();
            return this;
        }

        // This returns null if there is no name for the current node.  It
        // also skips the left brace that comes after the name.
        public string ReadName()
        {
            string next = tokens[tokenIndex++];
            if (next != "{")
            {
                tokenIndex++;
                return next;
            }
            return null;
        }

        public XFileTokenizer SkipToken()
        {
            tokenIndex++;
            return this;
        }

        public void Reset()
        {
            tokenIndex = 0;
            sb.Remove(0, sb.Length);
            index = 0;
            length = 0;
        }

        // Takes a string and turns it into an array of tokens.  This is created for performance
        // over readability.  It is far longer than it *needs* to be and 
        // uses a finite state machine to parse the tokens.
        private string[] TokensFromString(string ms)
        {
            // If we are currently in a state of the FSM such that we are building a token,
            // this is set to a positive value indicating the state.
            int groupnum = -1;
            // Each state in which we build a token is further broken up into substates.
            // This indicates the location in our current state.
            int groupLoc = 1;
            // Since we dont know before hand how big our token array is, and we want
            // it to pack nicely into an array, we can use is a list.
            List<string> strings = new List<string>();
            // The length of the string
            long msLength = ms.Length;

            for (int i = 0; i < msLength; i++)
            {
                // Current character
                int c = ms[i];
            // Yes, I used a goto.  They are generally ok in switch statements, although
            // I'm extending my welcome here.  The code goes to FSMSTART whenever
            // we have broken out of a state and want to transition to the start state
            // (that is, we are not currently building a token).  
            FSMSTART:
                switch (groupnum)
                {
                    // State in which we are building a number token
                    case 0:
                        switch (groupLoc)
                        {
                            // check if it has - sign
                            case 1:
                                if (c == '-')
                                {
                                    AddChar(c);
                                    groupLoc = 2;
                                    break;
                                }
                                else if (c >= '0' && c <= '9')
                                {
                                    AddChar(c);
                                    groupLoc = 3;
                                    break;
                                }
                                goto default;
                            // we are passed minus sign but before period
                            // A number most proceed a minus sign, but not necessarily
                            // precede a period without a minus sign.
                            case 2:
                                if (c >= '0' && c <= '9')
                                {
                                    AddChar(c);
                                    groupLoc = 3;
                                    break;
                                }
                                goto default;
                            // It is alright to accept a period now
                            case 3:
                                if (c >= '0' && c <= '9')
                                {
                                    AddChar(c);
                                    break;
                                }
                                else if (c == '.')
                                {
                                    AddChar(c);
                                    groupLoc = 4;
                                    break;
                                }
                                // we are done with the token because the next char
                                // is not part of a number
                                strings.Add(CurrentString);
                                ResetString();
                                groupLoc = 1;
                                groupnum = -1;
                                goto FSMSTART;
                            // we are just passed period, waiting for a number
                            case 4:
                                if (c >= '0' && c <= '9')
                                {
                                    groupLoc = 5;
                                    AddChar(c);
                                    break;
                                }
                                goto default;
                            // we are passed period and the number after it
                            case 5:
                                if (c >= '0' && c <= '9')
                                {
                                    AddChar(c);
                                    break;
                                }
                                strings.Add(CurrentString);
                                ResetString();
                                groupLoc = 1;
                                groupnum = -1;
                                goto FSMSTART;
                            // token does not make a valid number, ignore it and
                            // move on
                            default:
                                groupLoc = 1;
                                groupnum = -1;
                                ResetString();
                                break;

                        }
                        break;
                    // a string (may or may not start with " and ends with ") 
                    case 1:
                        switch (groupLoc)
                        {
                            // first  character
                            case 1:
                                AddChar(c);
                                groupLoc = 2;
                                break;
                            // now we can accept a wide variety of characters
                            case 2:
                                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                                    || c == '_' || (c >= '0' && c <= '9') || c == '.' || c == '-')
                                {
                                    AddChar(c);
                                    break;
                                }
                                // end of string
                                else if (c == '"')
                                {
                                    AddChar(c);
                                    strings.Add(CurrentString);
                                    ResetString();
                                    groupLoc = 1;
                                    groupnum = -1;
                                    break;
                                }

                                strings.Add(CurrentString);
                                ResetString();
                                groupLoc = 1;
                                groupnum = -1;
                                goto FSMSTART;
                            // token does not make a valid string; ignore and move on
                            default:
                                groupLoc = 1;
                                groupnum = -1;
                                ResetString();
                                break;
                        }
                        break;
                    // A constraint identifier OR array.  Read about X file format
                    // to see what this is (i.e.,  [...])
                    case 2:
                        switch (groupLoc)
                        {
                            // Read first char ([)
                            case 1:
                                AddChar(c);
                                groupLoc = 2;
                                break;
                            // can now accept letters or periods
                            case 2:
                                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                                {
                                    AddChar(c);
                                    groupLoc = 3;
                                    break;
                                }
                                else if (c == ' ' || c == '.')
                                {
                                    if (c != ' ')
                                        AddChar(c);
                                    groupLoc = 5;
                                    break;
                                }
                                // Since first token after [ is a #, this is an array
                                else if (c >= '0' && c <= '9')
                                {
                                    AddChar(c);
                                    groupLoc = 4;
                                    break;
                                }
                                goto default;
                            // passed first letter.  Can now accept a variety
                            // of chars
                            case 3:
                                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                                    || c == '_' || (c >= '0' && c <= '9'))
                                {
                                    AddChar(c);
                                    break;
                                }
                                else if (c == ']')
                                    goto case 10;
                                goto default;
                            // we are reading an array and can at this point only accept
                            // numbers
                            case 4:
                                if (c >= '0' && c <= '9')
                                {
                                    AddChar(c);
                                    break;
                                }
                                else if (c == ']')
                                    goto case 10;
                                goto default;
                            // can acept periods or spaces (open constraint identifier)
                            case 5:
                                if (c == '.' || c == ' ')
                                {
                                    AddChar(c);
                                    break;
                                }
                                else if (c == ']')
                                    goto case 10;
                                goto default;
                            // we have finished a valid token
                            case 10:
                                AddChar(c);
                                strings.Add(CurrentString);
                                ResetString();
                                groupLoc = 1;
                                groupnum = -1;
                                break;
                            // token is invalid
                            default:
                                ResetString();
                                groupLoc = 1;
                                groupnum = -1;
                                break;

                        }
                        break;
                    // A guid (starts with < ends with >)
                    case 3:
                        switch (groupLoc)
                        {
                            // first char (<)
                            case 1:
                                AddChar(c); ;
                                groupLoc = 2;
                                break;
                            // after first character can accept alphanumeric chars, spaces, and hyphens,
                            // but there must be at leaast on char between < and >
                            case 2:
                                if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                                    || c == '-' || c == ' ')
                                {
                                    if (c != ' ')
                                        AddChar(c);
                                    groupLoc = 3;
                                    break;
                                }
                                goto default;
                            // same as case 2 except we have read one token after start
                            case 3:
                                if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                                    || c == '-' || c == ' ')
                                {
                                    if (c != ' ')
                                        AddChar(c);
                                    break;
                                }
                                // valid GUID
                                else if (c == '>')
                                {
                                    AddChar(c);
                                    strings.Add(CurrentString);
                                    ResetString();
                                    groupLoc = 1;
                                    groupnum = -1;
                                    break;
                                }
                                goto default;
                            // invalid token
                            default:
                                ResetString();
                                groupLoc = 1;
                                groupnum = -1;
                                break;

                        }
                        break;
                    // reset group location on new line after reading a comment
                    case 5:
                        if (c == '\n')
                        {
                            groupLoc = 1;
                            groupnum = -1;
                        }
                        break;
                    case -1:
                        // characters that comprise tokens alone
                        if (c == ';' || c == ',' || c == '{' || c == '}')
                        {
                            strings.Add(((char)c).ToString());
                            ResetString();
                            groupLoc = 1;
                            groupnum = -1;
                            break;
                        }
                        // an array or constraint identfiier
                        else if (c == '[')
                        {
                            groupnum = 2;
                            goto case 2;
                        }
                        // a guid
                        else if (c == '<')
                        {
                            groupnum = 3;
                            goto case 3;
                        }
                        // a string or name
                        else if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                            || c == '"')
                        {
                            groupnum = 1;
                            goto case 1;
                        }
                        // a number
                        else if ((c == '-') || (c >= '0' && c <= '9'))
                        {
                            groupnum = 0;
                            goto case 0;
                        }
                        // a comment
                        else if (c == '/' || c == '#')
                        {
                            groupnum = 5;
                            goto case 5;
                        }
                        break;
                    default:
                        break;
                }
            }
            return strings.ToArray();
        }

        #endregion
    }
}
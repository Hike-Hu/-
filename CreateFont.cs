using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using System.IO;

public class CreateFont : Editor
{
    private static Font targetFont;
    private static TextAsset fntData;
    private static Material fontMaterial;
    private static Texture2D fontTexture;

    private static BMFont bmFont = new BMFont();

    private static void NewFont()
    {
        targetFont = null;
        fontMaterial = null;

        var needName = "";
        Object[] objRootPass = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.TopLevel);
        var rootPass = AssetDatabase.GetAssetPath(objRootPass[0]);
        //Debug.Log("目录：" + rootPass);//选中目录
        DirectoryInfo direction = new DirectoryInfo(rootPass);//获取文件夹，exportPath是文件夹的路径
        FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
        foreach (var t in files)
        {
            //判断文件的后缀
            if (t.Name.EndsWith(".png"))
            {
                fontTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    $"{rootPass}/{t.Name}");
                //Debug.Log("exefiles[i].Name名字为：" + t.Name);//exe文件名
                //Debug.Log("exefiles[i].FullName名字为：" + files[i].FullName);//exe文件的全部路径名
                //Debug.Log("exefiles[i].DirectoryName名字为：" + files[i].DirectoryName);//exe所在文件夹的文件夹路径
            }
            //判断文件的后缀
            if (t.Name.EndsWith(".fnt"))
            {
                needName = t.Name.Split('.')[0];
                fntData = AssetDatabase.LoadAssetAtPath<TextAsset>(
                $"{rootPass}/{t.Name}");
                //Debug.Log("exefiles[i].Name名字为：" + t.Name);//exe文件名
            }
            if (t.Name.EndsWith(".fontsettings"))
            {
                targetFont = AssetDatabase.LoadAssetAtPath<Font>(
                    $"{rootPass}/{t.Name}");
            }
            if (t.Name.EndsWith(".mat"))
            {
                fontMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                    $"{rootPass}/{t.Name}");
            }
        }

        if (targetFont == null)
        {
            targetFont = new Font();
            AssetDatabase.CreateAsset(targetFont, rootPass + "/" + needName + ".fontsettings");
        }

        if (fontMaterial == null)
        {
            fontMaterial = new Material(Shader.Find("GUI/Text Shader"));
            AssetDatabase.CreateAsset(fontMaterial, rootPass + "/" + needName + ".mat");
        }
        else
        {
            fontMaterial.shader = Shader.Find("GUI/Text Shader");
        }
    }
    [MenuItem("Assets/CreateBMFont")]
    public static void CreateBmFont()
    {
        NewFont();

        BMFontReader.Load(bmFont, fntData.name, fntData.bytes); // 借用NGUI封装的读取类
        CharacterInfo[] characterInfo = new CharacterInfo[bmFont.glyphs.Count];
        for (int i = 0; i < bmFont.glyphs.Count; i++)
        {
            BMGlyph bmInfo = bmFont.glyphs[i];
            CharacterInfo info = new CharacterInfo();
            info.index = bmInfo.index;
            info.uv.x = (float)bmInfo.x / (float)bmFont.texWidth;
            info.uv.y = 1 - (float)bmInfo.y / (float)bmFont.texHeight;
            info.uv.width = (float)bmInfo.width / (float)bmFont.texWidth;
            info.uv.height = -1f * (float)bmInfo.height / (float)bmFont.texHeight;
            info.vert.x = 0;
            info.vert.y = -(float)bmInfo.height;
            info.vert.width = (float)bmInfo.width;
            info.vert.height = (float)bmInfo.height;
            info.width = (float)bmInfo.advance;
            characterInfo[i] = info;
        }
        targetFont.characterInfo = characterInfo;
        if (fontMaterial)
        {
            fontMaterial.mainTexture = fontTexture;
        }
        targetFont.material = fontMaterial;

        EditorUtility.SetDirty(targetFont);

        Debug.Log("create font <" + targetFont.name + "> success");
    }

public class BMFont
{
[HideInInspector] [SerializeField] int mSize = 16;          // How much to move the cursor when moving to the next line
[HideInInspector] [SerializeField] int mBase = 0;           // Offset from the top of the line to the base of each character
[HideInInspector] [SerializeField] int mWidth = 0;          // Original width of the texture
[HideInInspector] [SerializeField] int mHeight = 0;         // Original height of the texture
[HideInInspector] [SerializeField] string mSpriteName;

// List of serialized glyphs
[HideInInspector] [SerializeField] List<BMGlyph> mSaved = new List<BMGlyph>();

// Actual glyphs that we'll be working with are stored in a dictionary, making the lookup faster
Dictionary<int, BMGlyph> mDict = new Dictionary<int, BMGlyph>();

/// <summary>
/// Whether the font can be used.
/// </summary>

public bool isValid { get { return (mSaved.Count > 0); } }

/// <summary>
/// Size of this font (for example 32 means 32 pixels).
/// </summary>

public int charSize { get { return mSize; } set { mSize = value; } }

/// <summary>
/// Base offset applied to characters.
/// </summary>

public int baseOffset { get { return mBase; } set { mBase = value; } }

/// <summary>
/// Original width of the texture.
/// </summary>

public int texWidth { get { return mWidth; } set { mWidth = value; } }

/// <summary>
/// Original height of the texture.
/// </summary>

public int texHeight { get { return mHeight; } set { mHeight = value; } }

/// <summary>
/// Number of valid glyphs.
/// </summary>

public int glyphCount { get { return isValid ? mSaved.Count : 0; } }

/// <summary>
/// Original name of the sprite that the font is expecting to find (usually the name of the texture).
/// </summary>

public string spriteName { get { return mSpriteName; } set { mSpriteName = value; } }

/// <summary>
/// Access to BMFont's entire set of glyphs.
/// </summary>

public List<BMGlyph> glyphs { get { return mSaved; } }

/// <summary>
/// Helper function that retrieves the specified glyph, creating it if necessary.
/// </summary>

public BMGlyph GetGlyph(int index, bool createIfMissing)
{
    // Get the requested glyph
    BMGlyph glyph = null;

    if (mDict.Count == 0)
    {
        // Populate the dictionary for faster access
        for (int i = 0, imax = mSaved.Count; i < imax; ++i)
        {
            BMGlyph bmg = mSaved[i];
            mDict.Add(bmg.index, bmg);
        }
    }

    // Saved check is here so that the function call is not needed if it's true
    if (!mDict.TryGetValue(index, out glyph) && createIfMissing)
    {
        glyph = new BMGlyph();
        glyph.index = index;
        mSaved.Add(glyph);
        mDict.Add(index, glyph);
    }
    return glyph;
}

/// <summary>
/// Retrieve the specified glyph, if it's present.
/// </summary>

public BMGlyph GetGlyph(int index) { return GetGlyph(index, false); }

/// <summary>
/// Clear the glyphs.
/// </summary>

public void Clear()
{
    mDict.Clear();
    mSaved.Clear();
}

/// <summary>
/// Trim the glyphs, ensuring that they will never go past the specified bounds.
/// </summary>

public void Trim(int xMin, int yMin, int xMax, int yMax)
{
    if (isValid)
    {
        for (int i = 0, imax = mSaved.Count; i < imax; ++i)
        {
            BMGlyph glyph = mSaved[i];
            if (glyph != null) glyph.Trim(xMin, yMin, xMax, yMax);
        }
    }
}
}
public class BMGlyph
{
public int index;   // Index of this glyph (used by BMFont)
public int x;       // Offset from the left side of the texture to the left side of the glyph
public int y;       // Offset from the top of the texture to the top of the glyph
public int width;   // Glyph's width in pixels
public int height;  // Glyph's height in pixels
public int offsetX; // Offset to apply to the cursor's left position before drawing this glyph
public int offsetY; // Offset to apply to the cursor's top position before drawing this glyph
public int advance; // How much to move the cursor after printing this character
public int channel; // Channel mask (in most cases this will be 15 (RGBA, 1+2+4+8)
public List<int> kerning;

/// <summary>
/// Retrieves the special amount by which to adjust the cursor position, given the specified previous character.
/// </summary>

public int GetKerning(int previousChar)
{
    if (kerning != null && previousChar != 0)
    {
        for (int i = 0, imax = kerning.Count; i < imax; i += 2)
            if (kerning[i] == previousChar)
                return kerning[i + 1];
    }
    return 0;
}

/// <summary>
/// Add a new kerning entry to the character (or adjust an existing one).
/// </summary>

public void SetKerning(int previousChar, int amount)
{
    if (kerning == null) kerning = new List<int>();

    for (int i = 0; i < kerning.Count; i += 2)
    {
        if (kerning[i] == previousChar)
        {
            kerning[i + 1] = amount;
            return;
        }
    }

    kerning.Add(previousChar);
    kerning.Add(amount);
}

/// <summary>
/// Trim the glyph, given the specified minimum and maximum dimensions in pixels.
/// </summary>

public void Trim(int xMin, int yMin, int xMax, int yMax)
{
    int x1 = x + width;
    int y1 = y + height;

    if (x < xMin)
    {
        int offset = xMin - x;
        x += offset;
        width -= offset;
        offsetX += offset;
    }

    if (y < yMin)
    {
        int offset = yMin - y;
        y += offset;
        height -= offset;
        offsetY += offset;
    }

    if (x1 > xMax) width -= x1 - xMax;
    if (y1 > yMax) height -= y1 - yMax;
}
}
public static class BMFontReader
{
/// <summary>
/// Helper function that retrieves the string value of the key=value pair.
/// </summary>

static string GetString(string s)
{
    int idx = s.IndexOf('=');
    return (idx == -1) ? "" : s.Substring(idx + 1);
}

/// <summary>
/// Helper function that retrieves the integer value of the key=value pair.
/// </summary>

static int GetInt(string s)
{
    int val = 0;
    string text = GetString(s);
#if UNITY_FLASH
try { val = int.Parse(text); } catch (System.Exception) { }
#else
    int.TryParse(text, out val);
#endif
    return val;
}

/// <summary>
/// Reload the font data.
/// </summary>

static public void Load(BMFont font, string name, byte[] bytes)
{
    font.Clear();

    if (bytes != null)
    {
        ByteReader reader = new ByteReader(bytes);
        char[] separator = new char[] { ' ' };

        while (reader.canRead)
        {
            string line = reader.ReadLine();
            if (string.IsNullOrEmpty(line)) break;
            string[] split = line.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);
            int len = split.Length;

            if (split[0] == "char")
            {
                // Expected data style:
                // char id=13 x=506 y=62 width=3 height=3 xoffset=-1 yoffset=50 xadvance=0 page=0 chnl=15

                int channel = (len > 10) ? GetInt(split[10]) : 15;

                if (len > 9 && GetInt(split[9]) > 0)
                {
                    UnityEngine.Debug.LogError("Your font was exported with more than one texture. Only one texture is supported by NGUI.\n" +
                        "You need to re-export your font, enlarging the texture's dimensions until everything fits into just one texture.");
                    break;
                }

                if (len > 8)
                {
                    int id = GetInt(split[1]);
                    BMGlyph glyph = font.GetGlyph(id, true);

                    if (glyph != null)
                    {
                        glyph.x = GetInt(split[2]);
                        glyph.y = GetInt(split[3]);
                        glyph.width = GetInt(split[4]);
                        glyph.height = GetInt(split[5]);
                        glyph.offsetX = GetInt(split[6]);
                        glyph.offsetY = GetInt(split[7]);
                        glyph.advance = GetInt(split[8]);
                        glyph.channel = channel;
                    }
                    else UnityEngine.Debug.Log("Char: " + split[1] + " (" + id + ") is NULL");
                }
                else
                {
                    Debug.LogError("Unexpected number of entries for the 'char' field (" + name + ", " + split.Length + "):\n" + line);
                    break;
                }
            }
            else if (split[0] == "kerning")
            {
                // Expected data style:
                // kerning first=84 second=244 amount=-5 

                if (len > 3)
                {
                    int first = GetInt(split[1]);
                    int second = GetInt(split[2]);
                    int amount = GetInt(split[3]);

                    BMGlyph glyph = font.GetGlyph(second, true);
                    if (glyph != null) glyph.SetKerning(first, amount);
                }
                else
                {
                    Debug.LogError("Unexpected number of entries for the 'kerning' field (" +
                        name + ", " + split.Length + "):\n" + line);
                    break;
                }
            }
            else if (split[0] == "common")
            {
                // Expected data style:
                // common lineHeight=64 base=51 scaleW=512 scaleH=512 pages=1 packed=0 alphaChnl=1 redChnl=4 greenChnl=4 blueChnl=4

                if (len > 5)
                {
                    font.charSize = GetInt(split[1]);
                    font.baseOffset = GetInt(split[2]);
                    font.texWidth = GetInt(split[3]);
                    font.texHeight = GetInt(split[4]);

                    int pages = GetInt(split[5]);

                    if (pages != 1)
                    {
                        Debug.LogError("Font '" + name + "' must be created with only 1 texture, not " + pages);
                        break;
                    }
                }
                else
                {
                    Debug.LogError("Unexpected number of entries for the 'common' field (" +
                        name + ", " + split.Length + "):\n" + line);
                    break;
                }
            }
            else if (split[0] == "page")
            {
                // Expected data style:
                // page id=0 file="textureName.png"

                if (len > 2)
                {
                    font.spriteName = GetString(split[2]).Replace("\"", "");
                    font.spriteName = font.spriteName.Replace(".png", "");
                    font.spriteName = font.spriteName.Replace(".tga", "");
                }
            }
        }
    }
}
}
public class ByteReader
{
byte[] mBuffer;
int mOffset = 0;

public ByteReader(byte[] bytes) { mBuffer = bytes; }
public ByteReader(TextAsset asset) { mBuffer = asset.bytes; }

/// <summary>
/// Read the contents of the specified file and return a Byte Reader to work with.
/// </summary>

static public ByteReader Open(string path)
{
#if UNITY_EDITOR || (!UNITY_FLASH && !NETFX_CORE && !UNITY_WP8 && !UNITY_WP_8_1)
    FileStream fs = File.OpenRead(path);

    if (fs != null)
    {
        fs.Seek(0, SeekOrigin.End);
        byte[] buffer = new byte[fs.Position];
        fs.Seek(0, SeekOrigin.Begin);
        fs.Read(buffer, 0, buffer.Length);
        fs.Close();
        return new ByteReader(buffer);
    }
#endif
    return null;
}

/// <summary>
/// Whether the buffer is readable.
/// </summary>

public bool canRead { get { return (mBuffer != null && mOffset < mBuffer.Length); } }

/// <summary>
/// Read a single line from the buffer.
/// </summary>

static string ReadLine(byte[] buffer, int start, int count)
{
#if UNITY_FLASH
// Encoding.UTF8 is not supported in Flash :(
StringBuilder sb = new StringBuilder();

int max = start + UpdateAssetCount;

for (int i = start; i < max; ++i)
{
	byte byte0 = buffer[i];

	if ((byte0 & 128) == 0)
	{
		// If an UCS fits 7 bits, its coded as 0xxxxxxx. This makes ASCII character represented by themselves
		sb.Append((char)byte0);
	}
	else if ((byte0 & 224) == 192)
	{
		// If an UCS fits 11 bits, it is coded as 110xxxxx 10xxxxxx
		if (++i == UpdateAssetCount) break;
		byte byte1 = buffer[i];
		int ch = (byte0 & 31) << 6;
		ch |= (byte1 & 63);
		sb.Append((char)ch);
	}
	else if ((byte0 & 240) == 224)
	{
		// If an UCS fits 16 bits, it is coded as 1110xxxx 10xxxxxx 10xxxxxx
		if (++i == UpdateAssetCount) break;
		byte byte1 = buffer[i];
		if (++i == UpdateAssetCount) break;
		byte byte2 = buffer[i];

		if (byte0 == 0xEF && byte1 == 0xBB && byte2 == 0xBF)
		{
			// Byte Order Mark -- generally the first 3 bytes in a Windows-saved UTF-8 file. Skip it.
		}
		else
		{
			int ch = (byte0 & 15) << 12;
			ch |= (byte1 & 63) << 6;
			ch |= (byte2 & 63);
			sb.Append((char)ch);
		}
	}
	else if ((byte0 & 248) == 240)
	{
		// If an UCS fits 21 bits, it is coded as 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx 
		if (++i == UpdateAssetCount) break;
		byte byte1 = buffer[i];
		if (++i == UpdateAssetCount) break;
		byte byte2 = buffer[i];
		if (++i == UpdateAssetCount) break;
		byte byte3 = buffer[i];

		int ch = (byte0 & 7) << 18;
		ch |= (byte1 & 63) << 12;
		ch |= (byte2 & 63) << 6;
		ch |= (byte3 & 63);
		sb.Append((char)ch);
	}
}
return sb.ToString();
#else
    return Encoding.UTF8.GetString(buffer, start, count);
#endif
}

/// <summary>
/// Read a single line from the buffer.
/// </summary>

public string ReadLine() { return ReadLine(true); }

/// <summary>
/// Read a single line from the buffer.
/// </summary>

public string ReadLine(bool skipEmptyLines)
{
    int max = mBuffer.Length;

    // Skip empty characters
    if (skipEmptyLines)
    {
        while (mOffset < max && mBuffer[mOffset] < 32) ++mOffset;
    }

    int end = mOffset;

    if (end < max)
    {
        for (; ; )
        {
            if (end < max)
            {
                int ch = mBuffer[end++];
                if (ch != '\n' && ch != '\r') continue;
            }
            else ++end;

            string line = ReadLine(mBuffer, mOffset, end - mOffset - 1);
            mOffset = end;
            return line;
        }
    }
    mOffset = max;
    return null;
}

/// <summary>
/// Assume that the entire file is a collection of key/value pairs.
/// </summary>

public Dictionary<string, string> ReadDictionary()
{
    Dictionary<string, string> dict = new Dictionary<string, string>();
    char[] separator = new char[] { '=' };

    while (canRead)
    {
        string line = ReadLine();
        if (line == null) break;
        if (line.StartsWith("//")) continue;

#if UNITY_FLASH
	string[] split = line.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);
#else
        string[] split = line.Split(separator, 2, System.StringSplitOptions.RemoveEmptyEntries);
#endif

        if (split.Length == 2)
        {
            string key = split[0].Trim();
            string val = split[1].Trim().Replace("\\n", "\n");
            dict[key] = val;
        }
    }
    return dict;
}

static BetterList<string> mTemp = new BetterList<string>();

/// <summary>
/// Read a single line of Comma-Separated Values from the file.
/// </summary>

public BetterList<string> ReadCSV()
{
    mTemp.Clear();
    string line = "";
    bool insideQuotes = false;
    int wordStart = 0;

    while (canRead)
    {
        if (insideQuotes)
        {
            string s = ReadLine(false);
            if (s == null) return null;
            s = s.Replace("\\n", "\n");
            line += "\n" + s;
        }
        else
        {
            line = ReadLine(true);
            if (line == null) return null;
            line = line.Replace("\\n", "\n");
            wordStart = 0;
        }

        for (int i = wordStart, imax = line.Length; i < imax; ++i)
        {
            char ch = line[i];

            if (ch == ',')
            {
                if (!insideQuotes)
                {
                    mTemp.Add(line.Substring(wordStart, i - wordStart));
                    wordStart = i + 1;
                }
            }
            else if (ch == '"')
            {
                if (insideQuotes)
                {
                    if (i + 1 >= imax)
                    {
                        mTemp.Add(line.Substring(wordStart, i - wordStart).Replace("\"\"", "\""));
                        return mTemp;
                    }

                    if (line[i + 1] != '"')
                    {
                        mTemp.Add(line.Substring(wordStart, i - wordStart).Replace("\"\"", "\""));
                        insideQuotes = false;

                        if (line[i + 1] == ',')
                        {
                            ++i;
                            wordStart = i + 1;
                        }
                    }
                    else ++i;
                }
                else
                {
                    wordStart = i + 1;
                    insideQuotes = true;
                }
            }
        }

        if (wordStart < line.Length)
        {
            if (insideQuotes) continue;
            mTemp.Add(line.Substring(wordStart, line.Length - wordStart));
        }
        return mTemp;
    }
    return null;
}
}

public class BetterList<T>
    {
#if UNITY_FLASH

	List<T> mList = new List<T>();
	
	/// <summary>
	/// Direct access to the buffer. Note that you should not use its 'Length' parameter, but instead use BetterList.size.
	/// </summary>
	
	public T this[int i]
	{
		get { return mList[i]; }
		set { mList[i] = value; }
	}
	
	/// <summary>
	/// Compatibility with the non-flash syntax.
	/// </summary>
	
	public List<T> buffer { get { return mList; } }

	/// <summary>
	/// Direct access to the buffer's size. Note that it's only public for speed and efficiency. You shouldn't modify it.
	/// </summary>

	public int size { get { return mList.Count; } }

	/// <summary>
	/// For 'foreach' functionality.
	/// </summary>

	public IEnumerator<T> GetEnumerator () { return mList.GetEnumerator(); }

	/// <summary>
	/// Clear the array by resetting its size to zero. Note that the memory is not actually released.
	/// </summary>

	public void Clear () { mList.Clear(); }

	/// <summary>
	/// Clear the array and release the used memory.
	/// </summary>

	public void Release () { mList.Clear(); }

	/// <summary>
	/// Add the specified item to the end of the list.
	/// </summary>

	public void Add (T item) { mList.Add(item); }

	/// <summary>
	/// Insert an item at the specified index, pushing the entries back.
	/// </summary>

	public void Insert (int index, T item)
	{
		if (index > -1 && index < mList.Count) mList.Insert(index, item);
		else mList.Add(item);
	}

	/// <summary>
	/// Returns 'true' if the specified item is within the list.
	/// </summary>

	public bool Contains (T item) { return mList.Contains(item); }

	/// <summary>
	/// Return the index of the specified item.
	/// </summary>

	public int IndexOf (T item) { return mList.IndexOf(item); }

	/// <summary>
	/// Remove the specified item from the list. Note that RemoveAt() is faster and is advisable if you already know the index.
	/// </summary>

	public bool Remove (T item) { return mList.Remove(item); }

	/// <summary>
	/// Remove an item at the specified index.
	/// </summary>

	public void RemoveAt (int index) { mList.RemoveAt(index); }

	/// <summary>
	/// Remove an item from the end.
	/// </summary>

	public T Pop ()
	{
		if (buffer != null && size != 0)
		{
			T val = buffer[mList.Count - 1];
			mList.RemoveAt(mList.Count - 1);
			return val;
		}
		return default(T);
	}

	/// <summary>
	/// Mimic List's ToArray() functionality, except that in this case the list is resized to match the current size.
	/// </summary>

	public T[] ToArray () { return mList.ToArray(); }

	/// <summary>
	/// List.Sort equivalent.
	/// </summary>

	public void Sort (System.Comparison<T> comparer) { mList.Sort(comparer); }

#else

        /// <summary>
        /// Direct access to the buffer. Note that you should not use its 'Length' parameter, but instead use BetterList.size.
        /// </summary>

        public T[] buffer;

        /// <summary>
        /// Direct access to the buffer's size. Note that it's only public for speed and efficiency. You shouldn't modify it.
        /// </summary>

        public int size = 0;

        /// <summary>
        /// For 'foreach' functionality.
        /// </summary>

        [System.Diagnostics.DebuggerHidden]
        [System.Diagnostics.DebuggerStepThrough]
        public IEnumerator<T> GetEnumerator()
        {
            if (buffer != null)
            {
                for (int i = 0; i < size; ++i)
                {
                    yield return buffer[i];
                }
            }
        }

        /// <summary>
        /// Convenience function. I recommend using .buffer instead.
        /// </summary>

        [System.Diagnostics.DebuggerHidden]
        public T this[int i]
        {
            get { return buffer[i]; }
            set { buffer[i] = value; }
        }

        /// <summary>
        /// Helper function that expands the size of the array, maintaining the content.
        /// </summary>

        void AllocateMore()
        {
            T[] newList = (buffer != null) ? new T[Mathf.Max(buffer.Length << 1, 32)] : new T[32];
            if (buffer != null && size > 0) buffer.CopyTo(newList, 0);
            buffer = newList;
        }

        /// <summary>
        /// Trim the unnecessary memory, resizing the buffer to be of 'Length' size.
        /// Call this function only if you are sure that the buffer won't need to resize anytime soon.
        /// </summary>

        void Trim()
        {
            if (size > 0)
            {
                if (size < buffer.Length)
                {
                    T[] newList = new T[size];
                    for (int i = 0; i < size; ++i) newList[i] = buffer[i];
                    buffer = newList;
                }
            }
            else buffer = null;
        }

        /// <summary>
        /// Clear the array by resetting its size to zero. Note that the memory is not actually released.
        /// </summary>

        public void Clear() { size = 0; }

        /// <summary>
        /// Clear the array and release the used memory.
        /// </summary>

        public void Release() { size = 0; buffer = null; }

        /// <summary>
        /// Add the specified item to the end of the list.
        /// </summary>

        public void Add(T item)
        {
            if (buffer == null || size == buffer.Length) AllocateMore();
            buffer[size++] = item;
        }

        /// <summary>
        /// Insert an item at the specified index, pushing the entries back.
        /// </summary>

        public void Insert(int index, T item)
        {
            if (buffer == null || size == buffer.Length) AllocateMore();

            if (index > -1 && index < size)
            {
                for (int i = size; i > index; --i) buffer[i] = buffer[i - 1];
                buffer[index] = item;
                ++size;
            }
            else Add(item);
        }

        /// <summary>
        /// Returns 'true' if the specified item is within the list.
        /// </summary>

        public bool Contains(T item)
        {
            if (buffer == null) return false;
            for (int i = 0; i < size; ++i) if (buffer[i].Equals(item)) return true;
            return false;
        }

        /// <summary>
        /// Return the index of the specified item.
        /// </summary>

        public int IndexOf(T item)
        {
            if (buffer == null) return -1;
            for (int i = 0; i < size; ++i) if (buffer[i].Equals(item)) return i;
            return -1;
        }

        /// <summary>
        /// Remove the specified item from the list. Note that RemoveAt() is faster and is advisable if you already know the index.
        /// </summary>

        public bool Remove(T item)
        {
            if (buffer != null)
            {
                EqualityComparer<T> comp = EqualityComparer<T>.Default;

                for (int i = 0; i < size; ++i)
                {
                    if (comp.Equals(buffer[i], item))
                    {
                        --size;
                        buffer[i] = default(T);
                        for (int b = i; b < size; ++b) buffer[b] = buffer[b + 1];
                        buffer[size] = default(T);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Remove an item at the specified index.
        /// </summary>

        public void RemoveAt(int index)
        {
            if (buffer != null && index > -1 && index < size)
            {
                --size;
                buffer[index] = default(T);
                for (int b = index; b < size; ++b) buffer[b] = buffer[b + 1];
                buffer[size] = default(T);
            }
        }

        /// <summary>
        /// Remove an item from the end.
        /// </summary>

        public T Pop()
        {
            if (buffer != null && size != 0)
            {
                T val = buffer[--size];
                buffer[size] = default(T);
                return val;
            }
            return default(T);
        }

        /// <summary>
        /// Mimic List's ToArray() functionality, except that in this case the list is resized to match the current size.
        /// </summary>

        public T[] ToArray() { Trim(); return buffer; }

        //class Comparer : System.Collections.IComparer
        //{
        //    public System.Comparison<T> func;
        //    public int Compare (object x, object y) { return func((T)x, (T)y); }
        //}

        //Comparer mComp = new Comparer();

        /// <summary>
        /// List.Sort equivalent. Doing Array.Sort causes GC allocations.
        /// </summary>

        //public void Sort (System.Comparison<T> comparer)
        //{
        //    if (size > 0)
        //    {
        //        mComp.func = comparer;
        //        System.Array.Sort(buffer, 0, size, mComp);
        //    }
        //}

        /// <summary>
        /// List.Sort equivalent. Manual sorting causes no GC allocations.
        /// </summary>

        [System.Diagnostics.DebuggerHidden]
        [System.Diagnostics.DebuggerStepThrough]
        public void Sort(CompareFunc comparer)
        {
            int start = 0;
            int max = size - 1;
            bool changed = true;

            while (changed)
            {
                changed = false;

                for (int i = start; i < max; ++i)
                {
                    // Compare the two values
                    if (comparer(buffer[i], buffer[i + 1]) > 0)
                    {
                        // Swap the values
                        T temp = buffer[i];
                        buffer[i] = buffer[i + 1];
                        buffer[i + 1] = temp;
                        changed = true;
                    }
                    else if (!changed)
                    {
                        // Nothing has changed -- we can start here next time
                        start = (i == 0) ? 0 : i - 1;
                    }
                }
            }
        }

        /// <summary>
        /// Comparison function should return -1 if left is less than right, 1 if left is greater than right, and 0 if they match.
        /// </summary>

        public delegate int CompareFunc(T left, T right);
#endif
    }

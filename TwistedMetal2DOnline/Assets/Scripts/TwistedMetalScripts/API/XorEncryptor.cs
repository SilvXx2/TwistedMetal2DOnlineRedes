using System.Text;

public static class XorEncryptor
{
    private const byte DefaultByteKey = 60;
    private const string DefaultStringKey = "TwistedMetal_Room_Key";

    public static byte[] EncryptDecrypt(byte[] data, byte key = DefaultByteKey)
    {
        if (data == null)
        {
            return System.Array.Empty<byte>();
        }

        byte[] result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ key);
        }

        return result;
    }

    public static string EncryptDecrypt(string text, string key = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        if (string.IsNullOrEmpty(key))
        {
            key = DefaultStringKey;
        }

        StringBuilder result = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = (char)(text[i] ^ key[i % key.Length]);
            result.Append(c);
        }

        return result.ToString();
    }

    public static string EncryptToBase64(string plainText, string key = null)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return string.Empty;
        }

        string encrypted = EncryptDecrypt(plainText, key);
        byte[] bytes = Encoding.UTF8.GetBytes(encrypted);
        return System.Convert.ToBase64String(bytes);
    }

    public static string DecryptFromBase64(string base64Text, string key = null)
    {
        if (string.IsNullOrEmpty(base64Text))
        {
            return string.Empty;
        }

        byte[] bytes = System.Convert.FromBase64String(base64Text);
        string encrypted = Encoding.UTF8.GetString(bytes);
        return EncryptDecrypt(encrypted, key);
    }
}

using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

/// <summary>
/// binarySaveLoad for unity
/// : simple save/load(binary format)
/// </summary>
public class binarySaveLoad
{
    /// <summary>
    /// true .. archive zip
    /// </summary>
    public static bool           IsZipArchive = false;
    /// <summary>
    /// true .. perform simple encryption
    /// </summary>
    public static bool           IsSimpleEncryption = false;
    /// <summary>
    ///  user encrypt method
    /// </summary>
    public static Action<byte[]> UserEncrypt;
    /// <summary>
    ///  user decrypt method
    /// </summary>
    public static Action<byte[]> UserDecrypt;

    /// <summary>
    /// save/load result
    /// </summary>
    public enum eResult
    {
        Success,
        Error,
        FileNotFound,
        IllegalCheckSum,
    };

    /// <summary>
    /// delete file
    /// </summary>
    /// <param name="filename"></param>
    public static void Delete(string filename)
    {
        filename = Path.Combine(Application.persistentDataPath, filename);

        File.Delete(filename);
    }
    
    /// <summary>
    /// save data
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="data"></param>
    /// <returns>Success or not</returns>
    public static eResult Save(string filename, object data)
    {
        byte[]	bdata;

        filename = Path.Combine(Application.persistentDataPath, filename);

        using (MemoryStream ms = new MemoryStream())
        {
            BinaryFormatter writer = new BinaryFormatter();
            writer.Serialize(ms, data);

            bdata = new byte[ms.Length + 4];  // 4byte checksum

            ms.ToArray().CopyTo(bdata, 4);
            addCheckSum(bdata);
        }
        
        // ZIP 圧縮
        byte[]	filedata;

        if (IsZipArchive == false)
        {
            filedata = bdata;
        }
        else
        {
            using (MemoryStream ms = new MemoryStream())
            {
                try
                {
                    DeflateStream dstream = new DeflateStream(ms, CompressionMode.Compress, true);
                    dstream.Write(bdata, 0, bdata.Length); 
                    dstream.Close();

                    filedata = ms.ToArray();
                }
                catch (System.Exception ex)
                {
                    Debug.LogError(ex.Message);
                    return eResult.Error;
                }
            }
        }

        // 暗号化
        if (IsSimpleEncryption == true)
        {
            encrypt(filedata);
        }

        using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
        {
            try
            {
                fs.Write(filedata, 0, filedata.Length);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
                return eResult.Error;
            }
        }

        return eResult.Success;
    }

    /// <summary>
    /// load data
    /// </summary>
    /// <typeparam name="T">data class</typeparam>
    /// <param name="filename"></param>
    /// <param name="data"></param>
    /// <returns>Success or not</returns>
    public static eResult Load<T>(string filename, out T data) where T : class
    {
        data = null;

        byte[]	bdata;
        byte[]	filedata;

        filename = Path.Combine(Application.persistentDataPath, filename);

        if (File.Exists(filename) == false)
        {
            return eResult.FileNotFound;
        }

        using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
        {
            try
            {
                filedata = new byte[fs.Length];
                fs.Read(filedata, 0, filedata.Length);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
                return eResult.Error;
            }
        }
        
        // 複合化
        if (IsSimpleEncryption == true)
        {
            decrypt(filedata);
        }

        // ZIP 解凍
        if (IsZipArchive == false)
        {
            //
        }
        else
        {
            byte[] buffer = new byte[1024];

            using (MemoryStream outms = new MemoryStream())
            {
                using (MemoryStream ms = new MemoryStream(filedata))
                {
                    DeflateStream dstream = new DeflateStream(ms, CompressionMode.Decompress, true);

                    while (true)
                    {
                        int readSize = dstream.Read(buffer, 0, buffer.Length);
                        if (readSize == 0)
                        {
                            break;
                        }
                        outms.Write(buffer, 0, readSize);
                    }
                    dstream.Close();
                
                    filedata = outms.ToArray();
                }
            }
        }

        // チェックサムが正しいか確認
        if (collateCheckSum(filedata) == false)
        {
            Debug.LogError("illegal check sum");
            return eResult.IllegalCheckSum;
        }

        bdata = new byte[filedata.Length-4];
        // 5byte 目からデータ
        Array.Copy(filedata, 4, bdata, 0, bdata.Length);

        using (MemoryStream ms = new MemoryStream(bdata))
        {
            BinaryFormatter reader = new BinaryFormatter();
            data = (T)reader.Deserialize(ms);
        }

        return eResult.Success;
    }

    /// <summary>
    /// add CheckSum
    /// </summary>
    static void addCheckSum(byte[] data)
    {
        int csum = caculateCheckSum(data);

        // 頭 4byte にチェックサムをつける
        data[0] = (byte)((csum >> 24) & 0xff);
        data[1] = (byte)((csum >> 16) & 0xff);
        data[2] = (byte)((csum >>  8) & 0xff);
        data[3] = (byte)( csum        & 0xff);
    }

    /// <summary>
    /// collate CheckSum
    /// </summary>
    static bool collateCheckSum(byte[] data)
    {
        int csum = caculateCheckSum(data);
        int sum  = (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | (data[3]);

        return csum == sum;
    }

    static int caculateCheckSum(byte[] data)
    {
        int csum = 0;

        for (int i = 4; i < data.Length; i++)
        {
            csum  ^= data[i];
            csum <<= 1;
        }

        return csum;
    }

    /// <summary>
    /// encrypt method
    /// </summary>
    /// <param name="data">raw data</param>
    static void encrypt(byte[] data)
    {
        if (UserEncrypt != null)
        {
            UserEncrypt(data);
        }
        else
        {
            sampleEncrypt(data);
        }
    }

    /// <summary>
    /// decrypt method
    /// </summary>
    /// <param name="data">encrypt data</param>
    static void decrypt(byte[] data)
    {
        if (UserDecrypt != null)
        {
            UserDecrypt(data);
        }
        else
        {
            sampleEncrypt(data);
        }
    }

    /// <summary>
    /// encrypt sample
    /// </summary>
    static void sampleEncrypt(byte[] data)
    {
        for (int i = 0; i < data.Length ; i++)
        {
            data[i] ^= 0xff;
        }
    }

}

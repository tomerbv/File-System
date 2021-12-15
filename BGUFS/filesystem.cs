using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BGUFS
{
    class filesystem
    {
        List<string[]> head;
        int headSize;

        public filesystem()
        {
            head = new List<string[]>();
        }

        private bool exists(string fileSystemName)
        {
            if (!fileSystemName.StartsWith("BGUFS_"))
            {
                Console.WriteLine("Not a BGUFS file");
                return false;
            }
            if (File.Exists(fileSystemName))
                return true;
            Console.WriteLine("filesystem does not exist");
            return false;
        }
        private string[] StringToNode(string filedata)
        {
            string[] data = filedata.Split('>');
            if (data.Length != 5)
                return null;
            else
                return data;
        }
        private void decompress(string fileSystemName)
        {
            using (FileStream fs = File.OpenRead(fileSystemName))
            {
                byte[] headSizebyte = new byte[4];
                fs.Read(headSizebyte, 0, 4);
                int asByte = BitConverter.ToInt32(headSizebyte, 0);
                if (asByte == 0)
                    this.headSize = 1024;
                else
                    this.headSize = asByte;

                int content = fs.ReadByte();
                string filedata = "";
                if (content != -1)
                {
                    while (content != -1 && (char)content != '*')
                    {
                        if ((char)content == '\n')
                        {
                            head.Add(StringToNode(filedata));
                            filedata = "";
                        }
                        else
                        {
                            filedata += (char)content;
                        }
                        content = fs.ReadByte();
                    }
                }
            }
        }

        private byte[] NodeToByte(string[] node)
        {
            byte[] filename = Encoding.ASCII.GetBytes(node[0] + ">");
            byte[] filesize = Encoding.ASCII.GetBytes(node[1] + ">");
            byte[] date = Encoding.ASCII.GetBytes(node[2] + ">");
            byte[] type = Encoding.ASCII.GetBytes(node[3] + ">");
            byte[] address = Encoding.ASCII.GetBytes(node[4] + "\n");
            return filename.Concat(filesize).Concat(date).Concat(type).Concat(address).ToArray();
        }

        private void addToHead(string fileSystemName)
        {
            using (FileStream fs = new FileStream(fileSystemName, FileMode.Open))
            {
                byte[] clear = new byte[headSize];
                fs.Write(clear, 0, headSize);
                fs.Position = 0;
                byte[] headSizeByte = BitConverter.GetBytes(headSize);
                fs.Write(headSizeByte);
                for (int i = 0; i < head.Count(); i++)
                {
                    byte[] nodeAsByte = NodeToByte(head.ElementAt(i));
                    if (fs.Position + nodeAsByte.Length >= headSize)
                    {
                        fs.Close();
                        expandHead(fileSystemName);
                        return;
                    }
                    fs.Write(nodeAsByte);
                    if (fs.Position >= headSize)
                    {
                        continue;
                    }
                }
                fs.Position = headSize - 1;
                string barrier = "*";
                fs.WriteByte(Encoding.ASCII.GetBytes(barrier)[0]);
            }
        }

        private void expandHead(string fileSystemName)
        {
            using(FileStream fs = new FileStream(fileSystemName, FileMode.Open))
            {
                List<string[]> headCopy = new List<string[]>(head);
                headCopy.Sort(new comparebyAddress());
                int address = Int32.Parse(headCopy.ElementAt(0)[4]);
                int size = Int32.Parse(headCopy.ElementAt(0)[1]);
                byte[] toMove = new byte[size];
                int newAddress = (int) fs.Length;
                fs.Read(toMove, address, size);
                fs.Write(toMove, newAddress, size);
                updateOriginalValue(headCopy.ElementAt(0)[0], newAddress.ToString());
                this.headSize += size;
                addToHead(fileSystemName);
            }
        }

        public void create(string fileSystemName)
        {
            fileSystemName = "BGUFS_" + fileSystemName;
            File.Create(fileSystemName);
        }

        public void add(string fileSystemName, string arg1)
        {
            if (exists(fileSystemName) && File.Exists(arg1))
            {
                decompress(fileSystemName);
                for (int i = 0; i < head.Count(); i++)
                {
                    if (head.ElementAt(i)[0] == arg1)
                    {
                        Console.WriteLine("file already exist");
                        addToHead(fileSystemName);
                        return;
                    }
                }
                byte[] content = File.ReadAllBytes(arg1);
                string fileName = arg1;
                string dateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                string size = content.Length.ToString();
                string savedAt;
                using (FileStream fs = new FileStream(fileSystemName, FileMode.Open))
                {
                    long length = fs.Length;
                    if (length <= 1024)
                        savedAt = "1024";
                    else
                        savedAt = length.ToString();
                }
                string[] filedata = new string[5];
                filedata[0] = fileName;
                filedata[1] = size;
                filedata[2] = dateTime;
                filedata[3] = "regular";
                filedata[4] = savedAt;
                head.Add(filedata);
                addToHead(fileSystemName);
                using (FileStream fs = new FileStream(fileSystemName, FileMode.Open))
                {
                    fs.Position = fs.Length;
                    fs.Write(content);
                }
            }
            else
                Console.WriteLine("file does not exist");
        }

        public void remove(string fileSystemName, string arg1)
        {
            bool found = false;
            if (exists(fileSystemName))
            {
                decompress(fileSystemName);
                if (head.Count == 0)
                    Console.WriteLine("The file is empty");
                else
                {
                    for (int i = 0; i < head.Count(); i++)
                    {
                        if (head.ElementAt(i)[0] == arg1)
                        {
                            found = true;
                            using (FileStream fs = new FileStream(fileSystemName, FileMode.Open))
                            {
                                int size = Int32.Parse(head.ElementAt(i)[1]);
                                int address = Int32.Parse(head.ElementAt(i)[4]);
                                byte[] toFree = new byte[size];
                                fs.Position = address;
                                fs.Write(toFree,0,size);
                            }
                            head.Remove(head.ElementAt(i));
                            break;
                        }
                      
                    }
                    if (!found)
                    {
                        Console.WriteLine("file does not exist");
                        addToHead(fileSystemName);
                        return;
                    }
                    /*remove links*/
                    for (int i = 0; i < head.Count(); i++)
                    {
                        if (head.ElementAt(i)[3] == arg1)
                        {
                            head.Remove(head.ElementAt(i));
                            i--;
                        }
                    }
                }
                addToHead(fileSystemName);
            }
        }

        public void rename(string fileSystemName, string arg1, string arg2)
        {
            if (exists(fileSystemName))
            {
                bool namefound = false;
                bool found = false;
                decompress(fileSystemName);
                for (int i = 0; i < head.Count(); i++)
                {
                    if (head.ElementAt(i)[0] == arg2)
                    {
                        namefound = true;
                    }
                }
                for (int i = 0; i < head.Count(); i++)
                {
                    if (head.ElementAt(i)[0] == arg1)
                    {
                        found = true;
                        head.ElementAt(i)[0] = arg2;
                    }
                }
                if (found)
                {
                    if (namefound)
                    {
                        Console.WriteLine("file " + arg2 + " already exists");

                    }
                    else
                    {
                        addToHead(fileSystemName);
                    }
                }
                else
                {
                    Console.WriteLine("file does not exist");
                }

            }
        }

        public void extract(string fileSystemName, string arg1, string arg2)
        {
            bool found = false;
            if (exists(fileSystemName))
            {
                decompress(fileSystemName);
                if (head.Count == 0)
                    Console.WriteLine("The file is empty");
                else
                {
                    for (int i = 0; i < head.Count(); i++)
                    {
                        if (head.ElementAt(i)[0] == arg1)
                        {
                            found = true;
                            using (FileStream fsFrom = new FileStream(fileSystemName, FileMode.Open))
                            using (FileStream fsTo = new FileStream(arg2, FileMode.Create))
                            {
                                int size = Int32.Parse(head.ElementAt(i)[1]);
                                int address = Int32.Parse(head.ElementAt(i)[4]);
                                byte[] toSend = new byte[size];
                                fsFrom.Position = address;
                                fsFrom.Read(toSend, 0, size);
                                fsTo.Write(toSend);
                            }
                            break;
                        }
                        
                    }
                    if (!found)
                    {
                        Console.WriteLine("file does not exist");
                        addToHead(fileSystemName);
                        return;
                    }
                }
                addToHead(fileSystemName);
            }
        }

        public void dir(string fileSystemName)
        {
            if (exists(fileSystemName))
            {
                decompress(fileSystemName);
                string linkedName;
                if (head.Count != 0)
                {
                    for (int i = 0; i < head.Count(); i++)
                    {
                        if(head.ElementAt(i)[3] != "regular")
                            Console.WriteLine(head.ElementAt(i)[0] + "," + head.ElementAt(i)[1] + "," + head.ElementAt(i)[2] + ",link," + head.ElementAt(i)[3]);
                        else
                            Console.WriteLine(head.ElementAt(i)[0] + "," + head.ElementAt(i)[1] + "," + head.ElementAt(i)[2] + "," + head.ElementAt(i)[3]);
                    }
                }
                else
                {
                    Console.WriteLine("The file is empty");
                }
            }
        }


        public void hash(string fileSystemName, string arg1)
        {
            bool found = false;
            if (exists(fileSystemName))
            {
                decompress(fileSystemName);
                if (head.Count == 0)
                    Console.WriteLine("The file is empty");
                else
                {
                    for (int i = 0; i < head.Count(); i++)
                    {
                 
                        if (head.ElementAt(i)[0] == arg1)
                        {
                            found = true;
                            using (var md5Hash = MD5.Create())
                            using (FileStream fsFrom = new FileStream(fileSystemName, FileMode.Open))
                            {
                                int size = Int32.Parse(head.ElementAt(i)[1]);
                                int address = Int32.Parse(head.ElementAt(i)[4]);
                                byte[] toHash = new byte[size];
                                fsFrom.Position = address;
                                fsFrom.Read(toHash, 0, size);
                                byte[] hash = md5Hash.ComputeHash(toHash);
                                string res = BitConverter.ToString(hash).Replace("-", string.Empty);
                                Console.WriteLine(res);
                            }
                            break;
                        }
                    }
                    if (!found)
                    {
                        Console.WriteLine("file does not exist");
                        addToHead(fileSystemName);
                        return;
                    }
                }
                addToHead(fileSystemName);
            }
        }

        public void optimize(string fileSystemName)
        {
            if (exists(fileSystemName))
            {
                decompress(fileSystemName);
                if (head.Count == 0)
                    Console.WriteLine("The file is empty");
                else
                {
                    List<string[]> headCopy = new List<string[]>(head);
                    headCopy.Sort(new comparebyAddress());
                    //remove links
                    for (int i = 0; i < headCopy.Count(); i++)
                    {
                        if (headCopy.ElementAt(i)[3] != "regular")
                        {
                            headCopy.RemoveAt(i);
                            i--;
                        }
                    } 
              
                    int size = Int32.Parse(headCopy.ElementAt(0)[1]);
                    int address = Int32.Parse(headCopy.ElementAt(0)[4]);
                    /* clear before first file content */
                    using (FileStream fs = new FileStream(fileSystemName, FileMode.Open))
                    {
                        if (address != this.headSize)
                        {
                            byte[] content = new byte[size];
                            fs.Position = address;
                            fs.Read(content, 0, size);

                            address = this.headSize;
                            fs.Position = this.headSize;
                            fs.Write(content, 0, size);
                            headCopy.ElementAt(0)[4] = address.ToString();
                            updateOriginalValue(headCopy.ElementAt(0)[0], address.ToString());
                        }

                        for (int i = 0; i < headCopy.Count() - 1; i++)
                        {
                            size = Int32.Parse(headCopy.ElementAt(i)[1]);
                            address = Int32.Parse(headCopy.ElementAt(i)[4]);
                            int nextSize = Int32.Parse(headCopy.ElementAt(i + 1)[1]);
                            int nextAddress = Int32.Parse(headCopy.ElementAt(i + 1)[4]);
                            if (address + size != nextAddress)
                            {
                                byte[] content = new byte[nextSize];
                                fs.Position = nextAddress;
                                fs.Read(content, 0, nextSize);

                                nextAddress = size + address;
                                fs.Position = nextAddress;
                                fs.Write(content, 0, nextSize);
                                headCopy.ElementAt(i + 1)[4] = nextAddress.ToString();
                                updateOriginalValue(headCopy.ElementAt(i + 1)[0], nextAddress.ToString());
                                /* clear after last file content */
                            }
                            if (i == headCopy.Count() - 2)
                            {
                                fs.SetLength(nextAddress + nextSize);
                            }
                        }
                    }
                } 
                addToHead(fileSystemName);
            }
        }

        private void updateOriginalValue(string fileName, string address)
        {
            for (int i = 0; i < head.Count() - 1; i++)
            {
                if (head.ElementAt(i)[0] == fileName || head.ElementAt(i)[3] == fileName)
                    head.ElementAt(i)[4] = address;
            }
        }
        public class comparebyAddress : IComparer<string[]>

        {
            public int Compare(string[] x, string[] y)
            {
                return int.Parse(x[4]).CompareTo(int.Parse(y[4]));
            }
        }
        public class comparebyAB : IComparer<string[]>

        {
            public int Compare(string[] x, string[] y)
            {

                return string.Compare(x[0], y[0]);

            }
        }
        public class comparebySize : IComparer<string[]>

        {
            public int Compare(string[] x, string[] y)
            {
                return int.Parse(x[1]).CompareTo(int.Parse(y[1]));
            }
        }
        public class comparebydate : IComparer<string[]>

        {
            public int Compare(string[] x, string[] y)
            {
                DateTime date1 = DateTime.ParseExact(x[2],"dd/MM/yyyy HH:mm",null);
                DateTime date2 = DateTime.ParseExact(y[2],"dd/MM/yyyy HH:mm", null);
                return DateTime.Compare(date1, date2);

            }
        }



        public void sortAB(string fileSystemName)
        {
            if (exists(fileSystemName))
            {
                decompress(fileSystemName);
                if (head.Count == 0)
                    Console.WriteLine("The file is empty");
                else
                    head.Sort(new comparebyAB());
                addToHead(fileSystemName);
            }
        }

        public void sortDate(string fileSystemName)
        {
            if (exists(fileSystemName))
            {
                decompress(fileSystemName);
                if (head.Count == 0)
                    Console.WriteLine("The file is empty");
                else
                    head.Sort(new comparebydate());
                addToHead(fileSystemName);
            }
        }

        public void sortSize(string fileSystemName)
        {
            if (exists(fileSystemName))
            {
                decompress(fileSystemName);
                if (head.Count == 0)
                    Console.WriteLine("The file is empty");
                else
                    head.Sort(new comparebySize());
                addToHead(fileSystemName);
            }
        }


        public void addLink(string fileSystemName, string arg1, string arg2)
        {
            if (exists(fileSystemName))
            {
                decompress(fileSystemName);
                if (head.Count == 0)
                    Console.WriteLine("The file is empty");
                else
                {
                    string[] linkedTo = null;
                    for (int i = 0; i < head.Count(); i++)
                    {
                        if (head.ElementAt(i)[0] == arg1)
                        {
                            Console.WriteLine("file already exist");
                            addToHead(fileSystemName);
                            return;
                        }
                        if (head.ElementAt(i)[0] == arg2)
                        {
                            linkedTo = head.ElementAt(i);
                        }
                    }
                    if (linkedTo == null)
                    {
                        Console.WriteLine("file does not exist");
                        addToHead(fileSystemName);
                        return;
                    }
                    string[] filedata = new string[6];
                    filedata[0] = arg1;
                    filedata[1] = linkedTo[1];
                    filedata[2] = linkedTo[2];
                    filedata[3] = linkedTo[0];
                    filedata[4] = linkedTo[4];
                    head.Add(filedata);
                    
                }
                addToHead(fileSystemName);
            }
        }
    }
}

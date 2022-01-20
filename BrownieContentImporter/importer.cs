using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using BrownieEngine;
using BrownieContentImporter;

using LWrite = BrownieEngine.s_map;
using LInput = System.IO.BinaryReader;
using LOutput = BrownieEngine.s_map;

namespace BrownieContentImporter
{
    [ContentImporter(
        ".PLF", 
        DefaultProcessor = "PUPPET level processor", 
        DisplayName = "PUPPET Level Format - Processor")]
    public class importer : ContentImporter<LInput>
    {
        public override LInput Import(string filename, ContentImporterContext context)
        {
            return new LInput(File.Open(filename, FileMode.Open));
        }
    }

    [ContentProcessor(DisplayName = "PUPPET Level Format")]
    public class processor : ContentProcessor<LInput, LOutput>
    {
        public override LOutput Process(LInput input, ContentProcessorContext context)
        {
            context.Logger.LogMessage("Processing PLF");

            LOutput map = new LOutput();
            try
            {
                map.version = input.ReadString();
                context.Logger.LogMessage("Processing PLF");
                context.Logger.LogMessage("Processing tile sizes...");

                var s1 = input.ReadByte();
                var s2 = input.ReadByte();

                map.tileSizeX = s1;
                map.tileSizeY = s2;

                context.Logger.LogMessage("Processing map sizes...");
                var ws1 = input.ReadUInt16();
                var ws2 = input.ReadUInt16();

                map.mapSizeX = ws1;
                map.mapSizeY = ws2;

                context.Logger.LogMessage("Processing tiles...");

                ushort[] tiles = new ushort[ws1 * ws2];
                for (int i = 0; i < tiles.Length; i++)
                {
                    tiles[i] = input.ReadUInt16();
                }
                map.tiles = tiles;
                string tileSetName = input.ReadString();
                map.tileSetName = tileSetName;
                
                if ((int)(input.BaseStream.Position + sizeof(ushort)) > (int)input.BaseStream.Length)
                {
                    input.Close();
                    return map;
                }
                ushort leng = input.ReadUInt16();

                map.entities = new List<o_entity>();
                for (int i = 0; i < leng; i++)
                {
                    o_entity ent = new o_entity();
                    ushort id = input.ReadUInt16();
                    int x = input.ReadInt32();
                    int y = input.ReadInt32();
                    ushort label = input.ReadUInt16();

                    ent.id = id;
                    ent.position = new Point(x, y);
                    ent.labelToCall = label;

                    if (input.ReadBoolean())
                    {
                        ent.stringlist = new List<Tuple<string, string>>();
                        int lengthOfFlags = input.ReadInt16();
                        for (int i2 = 0; i2 < lengthOfFlags; i2++)
                        {
                            string st = input.ReadString();
                            short l = input.ReadInt16();
                            for (int i3 = 0; i3 < l; i3++)
                            {
                                Tuple<string, string> str = new Tuple<string, string>(st, input.ReadString());
                                Console.Out.WriteLine("Flag: " + str.Item1 + ", " + str.Item2);
                                ent.stringlist.Add(str);
                            }
                        }
                    }
                    map.entities.Add(ent);
                }
                input.Close();
                return map;
            }
            catch (Exception ex)
            {
                context.Logger.LogMessage("Error {0}", ex);
                throw;
            }
        }
    }

    [ContentTypeWriter]
    public class s_levelWriter : ContentTypeWriter<LWrite>
    {
        protected override void Write(ContentWriter bw, LOutput value)
        {
            bw.Write(value.version);
            bw.Write(value.tileSizeX);
            bw.Write(value.tileSizeY);
            bw.Write(value.mapSizeX);
            bw.Write(value.mapSizeY); 
            for (int i = 0; i < value.tiles.Length; i++) {
                bw.Write(value.tiles[i]);
            }
            bw.Write(Directory.GetCurrentDirectory() + value.tileSetName);
            bw.Write((ushort)value.entities.Count);


            foreach (o_entity ent in value.entities)
            {
                if (ent == null)
                    continue;
                //count++;
               // Console.Out.WriteLine("Entity " + count + " at " + bw.BaseStream.Position);
                bw.Write(ent.id);
               // Console.Out.WriteLine("Entity " + count + " id: " + ent.id + " at " + bw.BaseStream.Position);
                bw.Write(ent.position.X);
                bw.Write(ent.position.Y);
                //Console.Out.WriteLine("Entity " + count + " id: Position (" + ent.position.X + ", " + ent.position.Y + ") at " + bw.BaseStream.Position);
                bw.Write(ent.labelToCall);
                //Console.Out.WriteLine("Entity " + count + " id: label" + ent.labelToCall + " at " + bw.BaseStream.Position);

                //Flag if the string list exists
                bw.Write(ent.stringlist != null);
                Console.Out.WriteLine(ent.stringlist != null);

                //List Out all the entity's flags
                if (ent.stringlist != null)
                {
                    List<Tuple<string, string>> stri = ent.stringlist;
                    Tuple<string, string> current = ent.stringlist[0];

                    List<short> lengths = new List<short>();
                    List<List<Tuple<string, string>>> groupOfLists = new List<List<Tuple<string, string>>>();

                    //Find all common flag names and store their values incrementally
                    while (stri.Count > 0)
                    {
                        List<Tuple<string, string>> SameTuples = stri.FindAll(x => x.Item1 == current.Item1);
                        groupOfLists.Add(SameTuples);
                        lengths.Add((short)SameTuples.Count);
                        Console.Out.WriteLine(current.Item1 + " with length " + (short)SameTuples.Count);
                        stri.RemoveAll(x => x.Item1 == current.Item1);

                        if (stri.Count > 0)
                            current = stri[0];
                    }

                    bw.Write((short)groupOfLists.Count);
                    int leng = 0;
                    foreach (List<Tuple<string, string>> st in groupOfLists)
                    {
                        //Name
                        bw.Write(st[0].Item1);
                        Console.Out.WriteLine(st[0].Item1);
                        //Lenght
                        bw.Write(lengths[leng]);
                        Console.Out.WriteLine(lengths[leng]);
                        for (int i = 0; i < lengths[leng]; i++)
                        {
                            Console.Out.WriteLine(st[i].Item2);
                            bw.Write(st[i].Item2);
                        }
                        leng++;
                    }
                }
            }

        }
        /*
            for (int i = 0; i < value.entities.Count; i++)
            {
                o_entity ent = value.entities[i];
                if (ent == null)
                    continue;
                output.Write(ent.id);
                output.Write(ent.position.X);
                output.Write(ent.position.Y);
                output.Write(ent.labelToCall);

                //Flag if the string list exists
                output.Write(ent.stringlist != null);
                Console.Out.WriteLine(ent.stringlist != null);

                if (ent.stringlist != null)
                {
                    List<Tuple<string, string>> stri = ent.stringlist;
                    Tuple<string, string> current = ent.stringlist[0];

                    List<short> lengths = new List<short>();
                    List<List<Tuple<string, string>>> groupOfLists = new List<List<Tuple<string, string>>>();

                    while (stri.Count > 0)
                    {
                        List<Tuple<string, string>> SameTuples = stri.FindAll(x => x.Item1 == current.Item1);
                        groupOfLists.Add(SameTuples);
                        lengths.Add((short)SameTuples.Count);
                        Console.Out.WriteLine(current.Item1 + " with length " + (short)SameTuples.Count);
                        stri.RemoveAll(x => x.Item1 == current.Item1);

                        if (stri.Count > 0)
                            current = stri[0];
                    }

                    output.Write((short)groupOfLists.Count);
                    int leng = 0;
                    foreach (List<Tuple<string, string>> st in groupOfLists)
                    {
                        //Name
                        output.Write(st[0].Item1);
                        Console.Out.WriteLine(st[0].Item1);
                        //Lenght
                        output.Write(lengths[leng]);
                        Console.Out.WriteLine(lengths[leng]);
                        for (int i2 = 0; i2 < lengths[leng]; i2++)
                        {
                            Console.Out.WriteLine(st[i2].Item2);
                            output.Write(st[i].Item2);
                        }
                        leng++;
                    }
                }
            }
            */
        public override string GetRuntimeType(TargetPlatform targetPlatform)
        {
            return typeof(LOutput).AssemblyQualifiedName;
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return "BrownieEngine.s_levelreader, BrownieEngine";
        }
    }



}

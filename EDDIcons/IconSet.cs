﻿/*
 * Copyright © 2017 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */

 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Reflection;
using System.IO;
using System.Collections;
using System.IO.Compression;
using ExtendedControls;
using System.Windows.Forms;

namespace EDDiscovery.Icons
{
    public static class IconSet
    {
        private static Dictionary<string, Image> defaultIcons;
        private static Dictionary<string, Image> icons;

        static IconSet()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string[] resnames = asm.GetManifestResourceNames();
            defaultIcons = new Dictionary<string, Image>(StringComparer.InvariantCultureIgnoreCase);
            string basename = typeof(IconSet).Namespace + ".";

            foreach (string resname in resnames)
            {
                if (resname.StartsWith(basename) && resname.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
                {
                    string name = resname.Substring(basename.Length, resname.Length - basename.Length - 4);
                    Image img = Image.FromStream(asm.GetManifestResourceStream(resname));
                    img.Tag = name;
                    defaultIcons[name] = img;
                }
            }
        }

        private static void InitLegacyIcons()
        {
            icons["Legacy.settings"] = IconSet.GetIcon("Controls.Main.Tools.Settings");             // from use by action system..
            icons["Legacy.missioncompleted"] = IconSet.GetIcon("Journal.MissionCompleted");
        }

        public static void ResetIcons()
        {
            icons = defaultIcons.ToArray().ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.InvariantCultureIgnoreCase);
            InitLegacyIcons();
        }

        public static void LoadIconsFromDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                foreach (var file in Directory.EnumerateFiles(path, "*.png", SearchOption.AllDirectories))
                {
                    string name = file.Substring(path.Length + 1).Replace('/', '.').Replace('\\', '.');
                    Image img = null;

                    try
                    {
                        img = Image.FromFile(file);
                        img.Tag = name;
                    }
                    catch
                    {
                        // Ignore any bad images
                        continue;
                    }

                    icons[name] = img;
                }
            }
        }

        public static void LoadIconsFromZipFile(string path)
        {
            if (File.Exists(path))
            {
                using (var zipfile = ZipFile.Open(path, ZipArchiveMode.Read))
                {
                    foreach (var entry in zipfile.Entries)
                    {
                        if (entry.FullName.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string name = entry.FullName.Substring(0, entry.FullName.Length - 4).Replace('/', '.').Replace('\\', '.');
                            Image img = null;

                            try
                            {
                                using (var zipstrm = entry.Open())
                                {
                                    var memstrm = new MemoryStream(); // Image will own this
                                    zipstrm.CopyTo(memstrm);
                                    img = Image.FromStream(memstrm);
                                    img.Tag = name;
                                }
                            }
                            catch
                            {
                                // Ignore any bad images
                                continue;
                            }

                            icons[name] = img;
                        }
                    }
                }
            }
        }

        public static Image GetIcon(string name)
        {
            if (icons == null)      // seen designer barfing over this
                return null;

            if (!name.Contains("."))
            {
                name = "Legacy." + name;
            }

            //System.Diagnostics.Debug.WriteLine("ICON " + name);

            if (icons.ContainsKey(name))            // written this way so you can debug step it.
                return icons[name];
            else if (defaultIcons.ContainsKey(name))
                return defaultIcons[name];
            else
            {
                System.Diagnostics.Debug.WriteLine("**************************** ************************" + Environment.NewLine + " Missing Icon " + name);
                return defaultIcons["Legacy.star"];
            }
        }
    }
}

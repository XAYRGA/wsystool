﻿using System;
using System.Collections.Generic;
using System.Text;

namespace wsysbuilder
{
    public static class HelpManifest
    {
        public static void print_general()
        {
            Console.WriteLine(@"

Syntax:

wsysbuilder <operation> [args....]
wsysbuilder unpack      <wsFile>    <project file>
wsysbuilder pack        <projectFile>   <wsOutput>

Optional arguments:
        -encode-format <format> : Only when using 'pack', changes what format your custom sounds are saved into the .aw with
            Available Formats:
                adpcm4hle -- Balance quality and compatibility, doesn't work on gamecube hardware.
                adpcm4    -- Has occasional clicking and some artifacts, but with a significant quality reduction. Compatible with hardware and LLE. 
                pcm8      -- Best quality, compatible with real hardware, huge filesize, can be used only in moderation before sounds will stop playing. 

        -awpadding <bytes>      : Changes the padding inside of the .aw file (when repacking). Fixes compatibility with a few games. 

        -skip-transform         : Doesn't decode the .wav files whenever unpacking (must have an existing wavetable.json, used only for fast re-unpacking).

        -awpath  <path>         : Changes the directory to look for .AW files when unpacking.       


https://www.xayr.ga/tools/
https://github.com/XAYRGA/wsystool/tree/wsbuilder
                                                                                              

");
        }

    }
}

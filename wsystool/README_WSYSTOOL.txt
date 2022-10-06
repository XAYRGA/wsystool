db   d8b   db .d8888. db    db .d8888. d8888b. db    db d888888b db      d8888b. d88888b d8888b. 
88   I8I   88 88'  YP `8b  d8' 88'  YP 88  `8D 88    88   `88'   88      88  `8D 88'     88  `8D 
88   I8I   88 `8bo.    `8bd8'  `8bo.   88oooY' 88    88    88    88      88   88 88ooooo 88oobY' 
Y8   I8I   88   `Y8b.    88      `Y8b. 88~~~b. 88    88    88    88      88   88 88~~~~~ 88`8b   
`8b d8'8b d8' db   8D    88    db   8D 88   8D 88b  d88   .88.   88booo. 88  .8D 88.     88 `88. 
 `8b8' `8d8'  `8888Y'    YP    `8888Y' Y8888P' ~Y8888P' Y888888P Y88888P Y8888D' Y88888P 88   YD 

 Tool for modifying wavesystems in JSystem games. 



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
                                                                                              
888  888  8888b.  888  888 888d888   .d88b.   8888b.  
`Y8bd8P'     "88b 888  888 888P"    d88P"88b     "88b 
  X88K   .d888888 888  888 888      888  888 .d888888 
.d8""8b. 888  888 Y88b 888 888  d8b Y88b 888 888  888 
888  888 "Y888888  "Y88888 888  Y8P  "Y88888 "Y888888 
                       888               888          
                  Y8b d88P          Y8b d88P          
                   "Y88P"            "Y88P" 



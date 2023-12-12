# wsystool
 WAVESYSTEM modification toolkit for JSystem games


## How do I use it?



``` 
wsystool help

Syntax:
wsystool <operation> [args....]
wsystool unpack      <wsFile>    <project file>
wsystool pack        <projectFile>   <wsOutput>

Optional arguments:
        -waveout <path>     : Extracts all of the waves from the wavesystem into the specified folder, doesn't if not specified.

        -awpath  <path>     : Changes the directory to look for .AW files when unpacking, and to place .aw files when repacking.

```



# WSYS Projects

WSYSTool allows you to modify, replace, and add sounds are in a wavesystem.  When you export a wavesystem, the project's strucure will look like this. 

```
root
    |
    |
    \----- custom
    |
    \----- reference
    |
    \----- scenes
    |-wavetable.json
	|-wsys.json
	/
```

Here's a list of what all of the folders do.

### Reference + wavetable.json
You usually won't have to touch this folder. This folder is described by the 'wavetable.json'. It contains the 'default data' that is in the wavesystem, so if you don't have a custom sound, this is used to rebuild the wsys. If you want to manipulate the raw ADPCM data or parameters, that's when you should mess with these files. 
### Scenes/*.json 
This folder contains scene descriptors. Inside of these files are lists of waveID's. Different scenes appear in different parts of the game. If you want to make a sound available in a particular scene / level / screen, you'll need to add it to one of these. Otherwise, the sound will not be added to any .aw.
### Custom 
Any sounds in here will replace the ID's of another sound. 
For example, if you want to replace sound with the ID 27, you would put a file named "27.wav" in this foolder.

Optionally, you're able to prefix the sounds with a particular name, so for example, we could name the sound "27_succ.wav" instead.". Loop points will automatically be extracted from the waves, and imported, and the default ingame key will be "60". 
If you'd like custom filenames, loop points, or parameters, you can see the 'custom wavetables' example below. 


### Custom Wavetables
You're able to put a file named 'custom_wavetable.json' in your project folder to specify parameters for sounds.

``` 
{
    "2": {
        "Key": 60,
        "Format": "adpcm2",
        "FileName": "2_succ.wav"
    }
}
```

The above example will look for '2_succ.wav' and import it at key 60,  with the format 'adpcm2'. It will import as sound ID 2, or replace sound ID 2 as specified by the key on the JSON object. 


# cool people 
Here's some cool people who's code was either borrowed, or helped out directly with the project: 

Arookas - https://github.com/arookas

ZyphronG - https://github.com/ZyphronG

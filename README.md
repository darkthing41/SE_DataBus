# SE_DataBus
Space Engineers: Encapsulates a protocol to allow programs to communicate by shared static strings.

> This project is configured to be built within a separate IDE to allow for error checking, code completion etc..
> Only the section in `#region CodeEditor` should be copied into the Space Engineers editor. This region has been automatically extracted into the corresponding `txt` file.

This is a utility designed to be incoporated into other programs; by itself, it won't do anything interesting.

##Description

Encapsulates operations that allow data to be safely stored and retrieved through a shared text field (e.g. TextPanel fields).
- Static fields hold data for as long as it's needed
- Temporary fields operate as a FIFO queue

This allows for several behaviours not directly achieveable by parameterisation:
- Export data to be used by unspecified sources
  + e.g. a common system clock allowing synchronisation/timing
  + e.g. ship attitude/position, calculated once, used by different systems
- Allow multiple sources to send commands to one block
  + (Programs cannot execute more than once per physics tick, so multiple commands send at the same time get lost. This may be annoying, or downright dangerous.)
  + e.g. a program running off a 60Hz clock will not accept any new parameters as it is always busy running
- Allow programs to communicate at their own rates
  + parameters/commands may be passed independently of execution
- Allow programs to communicate across space
  + storage in block data can be saved, moved, and read back in entirely different ships or bases
- Inherently non-volatile
  + storage in block data isn't affected by re-initialisation
- More expressive parameter/command usage
  + no longer tied to combining paremeters into one string; easily queue multiple commands
  + easily have access to the most recent data without saving a copy locally each time
  
I'm sure there are plenty more creative things you could do with this.
  
##Hardware
| Block(s)      | number        | Configurable  |
| ------------- | ------------- | ------------- |
| Text Panel    | single        | variable; see below
(*`Text Panel` type includes LCD Screens)

##Configuration
- `Text Panel` `PublicText` is an example, but any string will work so long as the internal storage interface is updated (all 3 lines of it: read, write, append).
- `lengthId`: how many characters are required for the Id

##Extensibility
- Any type can be stored, so long as they can be encoded/decoded from a string, and ensured not to contain the record terminator. The main logic is encapsulated in procedures taking strings as arguments.
# FortniteReplayAnalyzer

- This is a program I personally use to analyze my Fortnite games.  I decided to publish it on Github because it's cool 8-).
- This project is based on the excellent [FortniteReplayDecompressor](https://github.com/Shiqan/FortniteReplayDecompressor) project.
- I've organized this project so that FortniteReplayDecompressor is a git submodule.  In this way if FortniteReplayDecompressor is updated I can easily catch up.
- In theory this project can be cloned and built with no further setup.  I used Visual Studio 2022 and .Net 6.0 for my environment.
- The program runs on the command line with no GUI.  It takes 2 mandatory arguments which is the full path to the folder containing the replay files and your player GUID.
- To initially clone this repository with the submodule:
```
git clone https://github.com/stevegee58/FortniteReplayAnalyzer.git
cd FortniteReplayAnalyzer
git submodule update --init --recursive
```
- To update the FortniteReplayDecompressor submodule to the latest:
```
git submodule update --remote
```
- To run the program:
```
FortniteReplayAnalyzer <replay folder location> <your player GUID>
```

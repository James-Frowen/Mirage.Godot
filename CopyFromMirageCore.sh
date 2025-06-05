## Assumes that Mirage.Core and Mirage.Godot are in the same root directory.
## If not path adjustments will be needed

if [ -z "$1" ]; then
  echo "Usage: bash ./CopyFromMirageCore.sh /path/to/Mirage.Core/"
  echo "Ex: bash ./CopyFromMirageCore.sh ../Mirage.Core/"
  exit 1
else
  MiragePath=$1
  echo "Copying Mirage,Core Updates to Mirage.Godot"
  echo "Mirage.Core Path:  $MiragePath"
fi 

CopyScripts () {
	# make the directory if it doesn't exist already...
	if [ ! -d "$2" ]; then
		echo "Creating directory: $2"
		mkdir -p "$2"
	#else
	#	rm $2/*.cs
	fi
    
    # add -v to cp for verbose logging
    find $1/*.cs | xargs cp -vt $2
}

echo
echo "Applying Mirage.Logging changes..."
CopyScripts "$MiragePath/src/Mirage.Logging" "./src/Mirage.Core/Mirage.Logging"

echo
echo "Applying Mirage.SocketLayer changes..."
CopyScripts "$MiragePath/src/Mirage.SocketLayer" "./src/Mirage.Core/Mirage.SocketLayer"
CopyScripts "$MiragePath/src/Mirage.SocketLayer/Connection" "./src/Mirage.Core/Mirage.SocketLayer/Connection"
CopyScripts "$MiragePath/src/Mirage.SocketLayer/ConnectionTrackers" "./src/Mirage.Core/Mirage.SocketLayer/ConnectionTrackers"
CopyScripts "$MiragePath/src/Mirage.SocketLayer/Enums" "./src/Mirage.Core/Mirage.SocketLayer/Enums"

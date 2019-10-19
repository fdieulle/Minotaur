# Minotaur
Distribution framework for timeseries data access and distributed tasks

# Layers

|----------|--------|-----|
| Recorder | Cursor |	  |
|----------|--------|	  |
|   Column stream   |	  |
|-------------------| IDb |
|        Codec      |	  |
|-------------------|	  |
|      IO stream    |	  |
|-------------------|-----|


# Meta
* One file by symbol.
* Column's meta contains the Name, Type, and the timeline. (TotalSize or statitics ?)
* Timeline indexer: Key[Symbol + Column] define the timeline and associated file by entry. 


# Todo List

* Move Meta data into a dedicated service and manage a synchronization point here to support multi threads and multi processes.
* Complete the FileTimeSeries DB implementation by implementing Insert and Delete methods based on recorder and cursors.
* Raise a scan regularly to merge the timeseries on persisted files and optimize the btree and number of files.
* Propose a remote implementation.
* Propose a distributed implementation.


# Dynamic Permissions
A horrible attempt at explaining how dynamic permissions work.

###### Prerequisites
1) A basic understanding of JSON
  * https://en.wikipedia.org/wiki/JSON#Data_types.2C_syntax_and_example
  * http://www.w3schools.com/json/json_syntax.asp
  
2) The base format for dynamic permissions
  * http://pastebin.com/YEQcUBmf
  
### The Format
The format consists of a `Role` and a `User` block. They both share the same data model of `Id: DynamicPermissionBlock`. The `Id` will vary depending on whether `DynamicPermissionBlock` is inside a `User` (User id) or `Role` (Role id) block. You can find out these ids by using `ued list` for user ids and `role list` for role ids.

* A `DynamicPermissionBlock` is made out of an `Allow` and a `Deny` `DynamicRestricionSets`. At first we evaluate `Allow` and then the `Deny` `DynamicRestricionSets`.

* A `DynamicRestricionSet` is made out of `Modules` and `Commands`. Both of these store "string:RestrictionData". The string is either the name of the command group, command, alias or module name we want to allow/deny.

* `RestrictionData` is made out of an array of channels the parent `DynamicRestricionSet` will be evaluated in and an `Error` string, which will be printed if the evaluation returned false.


#### Example
http://pastebin.com/qfekdmcM
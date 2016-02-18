# Dynamic Permissions
A horrible attempt at explaining how dynamic permissions work.

###### Prerequisites
1) A basic understanding of JSON
  * https://en.wikipedia.org/wiki/JSON#Data_types.2C_syntax_and_example
  * http://www.w3schools.com/json/json_syntax.asp
  
2) The base format for dynamic permissions
  * http://pastebin.com/YEQcUBmf
  
### The Format

###### Feel free to skip to the example section if this doesn't make sense


The format consists of a `Role` and a `User` block. They both share the same data model of `Id: DynamicPermissionBlock`. The `Id` will vary depending on whether `DynamicPermissionBlock` is inside a `User` (User id) or `Role` (Role id) block. You can find out these ids by using `ued list` for user ids and `role list` for role ids.

* A `DynamicPermissionBlock` is made out of an `Allow` and a `Deny` `DynamicRestricionSets`. At first we evaluate `Allow` and then the `Deny` `DynamicRestricionSets`.

* A `DynamicRestricionSet` is made out of `Modules` and `Commands`. Both of these store `string:RestrictionData`. The string is either the name of the command group, command, alias or module name we want to allow/deny.

* `RestrictionData` is made out of an array of channels the parent `DynamicRestricionSet` will be evaluated in and an `Error` string, which will be printed if the evaluation returned false.


#### Example

We are going to be creating dynamic permissions. Our goals here:

- Deny every dynamic permission command for the @everyone role.
- Enable every dynamic permission command for the "Trusted" role, except the "audio" module, "color" command group and "twitch disconnect", "terraria disconnect" commands.
- Enable the "VIP" role to use the "audio" module commands and the "color" command group.
- Enable the "Relay Mod" role to use "twitch disconnect" and "twitch connect" commands.
- Add a special case for the user "Billy" to allow him to use Twitch emotes in the channel "Billy-room".

##### Id table

###### Roles
Object | Id 
--- | ---
`@everyone` | `111`
`"Trusted"` | `222`
`"VIP"` | `333`
`Relay Mod` | `444`

###### Channels
Object | Id 
--- | ---
`#Billy-room` | `9999`

###### Users
Object | Id 
--- | ---
`@Billy` | `77777`

##### The procedure

1) Download the base format (http://pastebin.com/YEQcUBmf)

2) Disable dynperm commands for the @everyone role using the "Wildcard" `"*"`
```
{
    "Roles": {
        "Roles": {
            "111": {
                "Deny": {
                    "Modules": {
                        "*": {
                            "WhenInChannels": [],
                            "Error": "You need to be trusted to use dynamic permission commands."
                        }
                    }
                }
            }
        }
    },
    "Users": {
        "USER_ID": {
            "Allow": {
                "Modules": {
                    "Name": {
                        "WhenInChannels": [],
                        "Error": ""
                    }
                },
                "Commands": {
                    "Name": {
                        "WhenInChannels": [],
                        "Error": ""
                    }
                }
            },
            "Deny": {
                "Modules": {
                    "Name": {
                        "WhenInChannels": [],
                        "Error": ""
                    }
                },
                "Commands": {
                    "Name": {
                        "WhenInChannels": [],
                        "Error": ""
                    }
                }
            }
        }
    }
}
```
Let's add a `Error` message just for fun here as well.


3) Add the "Trusted" role and it's properties:

```
{
    "Roles": {
        "Roles": {
            "111": {
                "Deny": {
                    "Modules": {
                        "*": {
                            "WhenInChannels": [],
                            "Error": "You need to be trusted to use dynamic permission commands."
                        }
                    }
                }
            },
            "222": {
                "Allow": {
                    "Modules": {
                        "*": {
                            "WhenInChannels": [],
                            "Error": ""
                        }
                    }
                },
                "Deny": {
                    "Modules": {
                        "audio": {
                            "WhenInChannels": [],
                            "Error": ""
                        }
                    },
                    "Commands": {
                        "color": {
                            "WhenInChannels": [],
                            "Error": ""
                        },
                        "twitch disconnect": {
                            "WhenInChannels": [],
                            "Error": "You must be a relay mod to use this command."
                        },
                        "terraria disconnect": {
                            "WhenInChannels": [],
                            "Error": "You must be a relay mod to use this command."
                        }
                    }
                }
            }
        }
    },
    "Users": {
        "USER_ID": {
            "Allow": {
                "Modules": {
                    "Name": {
                        "WhenInChannels": [],
                        "Error": ""
                    }
                },
                "Commands": {
                    "Name": {
                        "WhenInChannels": [],
                        "Error": ""
                    }
                }
            },
            "Deny": {
                "Modules": {
                    "Name": {
                        "WhenInChannels": [],
                        "Error": ""
                    }
                },
                "Commands": {
                    "Name": {
                        "WhenInChannels": [],
                        "Error": ""
                    }
                }
            }
        }
    }
}
```

4) Add the "VIP" role and it's properties.

```
{
    "Roles": {
        "Roles": {
            "111": {
                "Deny": {
                    "Modules": {
                        "*": {
                            "WhenInChannels": [],
                            "Error": "You need to be trusted to use dynamic permission commands."
                        }
                    }
                }
            },
            "222": {
                "Allow": {
                    "Modules": {
                        "*": {
                            "WhenInChannels": [],
                            "Error": ""
                        }
                    }
                },
                "Deny": {
                    "Modules": {
                        "audio": {
                            "WhenInChannels": [],
                            "Error": ""
                        }
                    },
                    "Commands": {
                        "color": {
                            "WhenInChannels": [],
                            "Error": ""
                        },
                        "twitch disconnect": {
                            "WhenInChannels": [],
                            "Error": "You must be a relay mod to use this command."
                        },
                        "terraria disconnect": {
                            "WhenInChannels": [],
                            "Error": "You must be a relay mod to use this command."
                        }
                    }
                }
            },
            "333": {
                "Allow": {
                    "Modules": {
                        "audio": {
                            "WhenInChannels": [],
                            "Error": ""
                        }
                    },
                    "Commands": {
                        "color": {
                            "WhenInChannels": [],
                            "Error": ""
                        }
                    }
                }
            }
        }
    },
    "Users": {
        "USER_ID": {
            "Allow": {
                "Modules": {
                    "Name": {
                        "WhenInChannels": [],
                        "Error": ""
                    }
                },
                "Commands": {
                    "Name": {
                        "WhenInChannels": [],
                        "Error": ""
                    }
                }
            },
            "Deny": {
                "Modules": {
                    "Name": {
                        "WhenInChannels": [],
                        "Error": ""
                    }
                },
                "Commands": {
                    "Name": {
                        "WhenInChannels": [],
                        "Error": ""
                    }
                }
            }
        }
    }
}
```

5) Add the Relay Mod role and it's properties:

```
{
    "Roles": {
        "Roles": {
            "111": {
                "Deny": {
                    "Modules": {
                        "*": {
                            "WhenInChannels": [],
                            "Error": "You need to be trusted to use dynamic permission commands."
                        }
                    }
                }
            },
            "222": {
                "Allow": {
                    "Modules": {
                        "*": {
                            "WhenInChannels": [],
                            "Error": ""
                        }
                    }
                },
                "Deny": {
                    "Modules": {
                        "audio": {
                            "WhenInChannels": [],
                            "Error": ""
                        }
                    },
                    "Commands": {
                        "color": {
                            "WhenInChannels": [],
                            "Error": ""
                        },
                        "twitch disconnect": {
                            "WhenInChannels": [],
                            "Error": "You must be a relay mod to use this command."
                        },
                        "terraria disconnect": {
                            "WhenInChannels": [],
                            "Error": "You must be a relay mod to use this command."
                        }
                    }
                }
            },
            "333": {
                "Allow": {
                    "Modules": {
                        "audio": {
                            "WhenInChannels": [],
                            "Error": ""
                        }
                    },
                    "Commands": {
                        "color": {
                            "WhenInChannels": [],
                            "Error": ""
                        }
                    }
                }
            },
            "444": {
                "Allow": {
                    "Commands": {
                        "twitch disconnect": {
                            "WhenInChannels": [],
                            "Error": ""
                        },
                        "terraria disconnect": {
                            "WhenInChannels": [],
                            "Error": ""
                        }
                    }
                }
            }
        }
    },
    "Users": {
        "USER_ID": {
            "Allow": {
                "Modules": {
                    "Name": {
                        "WhenInChannels": [],
                        "Error": ""
                    }
                },
                "Commands": {
                    "Name": {
                        "WhenInChannels": [],
                        "Error": ""
                    }
                }
            },
            "Deny": {
                "Modules": {
                    "Name": {
                        "WhenInChannels": [],
                        "Error": ""
                    }
                },
                "Commands": {
                    "Name": {
                        "WhenInChannels": [],
                        "Error": ""
                    }
                }
            }
        }
    }
}
```

6) Add Billy and his channel including their properties:

```
{
    "Roles": {
        "Roles": {
            "111": {
                "Deny": {
                    "Modules": {
                        "*": {
                            "WhenInChannels": [],
                            "Error": "You need to be trusted to use dynamic permission commands."
                        }
                    }
                }
            },
            "222": {
                "Allow": {
                    "Modules": {
                        "*": {
                            "WhenInChannels": [],
                            "Error": ""
                        }
                    }
                },
                "Deny": {
                    "Modules": {
                        "audio": {
                            "WhenInChannels": [],
                            "Error": ""
                        }
                    },
                    "Commands": {
                        "color": {
                            "WhenInChannels": [],
                            "Error": ""
                        },
                        "twitch disconnect": {
                            "WhenInChannels": [],
                            "Error": "You must be a relay mod to use this command."
                        },
                        "terraria disconnect": {
                            "WhenInChannels": [],
                            "Error": "You must be a relay mod to use this command."
                        }
                    }
                }
            },
            "333": {
                "Allow": {
                    "Modules": {
                        "audio": {
                            "WhenInChannels": [],
                            "Error": ""
                        }
                    },
                    "Commands": {
                        "color": {
                            "WhenInChannels": [],
                            "Error": ""
                        }
                    }
                }
            },
            "444": {
                "Allow": {
                    "Commands": {
                        "twitch disconnect": {
                            "WhenInChannels": [],
                            "Error": ""
                        },
                        "terraria disconnect": {
                            "WhenInChannels": [],
                            "Error": ""
                        }
                    }
                }
            }
        }
    },
    "Users": {
        "77777": {
            "Allow": {
                "Modules": {
                    "twitch emotes": {
                        "WhenInChannels": [ 9999 ],
                        "Error": "Billy you can only use twitch emotes in the Billy channel."
                    }
                }
            }
        }
    }
}
```

7) Now call }dynperm set <data> and the changes should be present.

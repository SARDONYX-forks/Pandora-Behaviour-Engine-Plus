﻿using System;
using System.Collections.Generic;
using System.IO;
using Pandora.API.Patch;

namespace Pandora.Models.Patch.Mod;

public class NemesisModInfo : IModInfo
{
	public override int GetHashCode()
	{
		return Code.GetHashCode();
	}
	public bool Equals(IModInfo? other)
	{
		return other != null && Code == other.Code &&
			Version == other.Version;
	}
	public bool Active { get; set; } = false;

	public DirectoryInfo Folder { get; private set; }

	public Dictionary<string, string> StringProperties { get; private set; } = [];

	public string Name { get; private set; } = "Default";

	public string Author { get; private set; } = "Default";
	public string URL { get; private set; } = "Default";

	public string Code { get; private set; } = "Default";

	public Version Version { get; } = new Version(1, 0, 0);

	public IModInfo.ModFormat Format { get; } = IModInfo.ModFormat.Nemesis;

	public uint Priority { get; set; } = 0;

	//internal string Auto { get; set; } = "Default";
	//internal string RequiredFile { get; set; } = "Default";
	//internal string FileToCopy { get; set; } = "Default";
	//internal bool Hidden { get; set; } = false;
	public bool Valid { get; private set; } = false;

	public NemesisModInfo()
	{
		Valid = false;
		Folder = new DirectoryInfo(Environment.CurrentDirectory);
	}
	public NemesisModInfo(DirectoryInfo folder, string name, string author, string url, bool active, Dictionary<string, string> properties)
	{
		Folder = folder;
		Name = name;
		Author = author;
		URL = url;
		StringProperties = properties;
		Code = Folder.Name;
		Valid = true;
		Active = active;
	}
	public static NemesisModInfo ParseMetadata(FileInfo file)
	{
		Dictionary<string, string> properties = [];

		if (!file.Exists)
		{
			return new NemesisModInfo();
		}
		using (StreamReader reader = new(file.FullName))
		{
			string s;
			string[] args;

			while ((s = reader.ReadLine()!) != null)
			{
				args = s.Split("=");
				if (args.Length > 1)
				{
					properties.Add(args[0].ToLower().Trim(), args[1].Trim());
				}
			}
		}
		properties.TryGetValue("name", out string? name);
		properties.TryGetValue("author", out string? author);
		properties.TryGetValue("site", out string? url);
		properties.TryGetValue("hidden", out string? hidden);

		bool.TryParse(hidden, out bool active);


		return name != null && author != null && url != null ? new NemesisModInfo(file.Directory!, name, author, url, active, properties) : new NemesisModInfo();
		//add metadata later
	}
}

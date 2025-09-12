﻿// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2023-2025 Pandora Behaviour Engine Contributors

using HKX2E;
using NLog;
using Pandora.API.Patch.IOManagers;
using Pandora.Models.Patch.Skyrim64.Hkx.Packfile;
using Pandora.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace Pandora.Models.Patch.IO.Skyrim64;

public class PackFileExporter : IMetaDataExporter<PackFile>
{
	private static readonly FileInfo PreviousOutputFile = PandoraPaths.PreviousOutputFile;
	public DirectoryInfo ExportDirectory { get; set; }


	private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

	public PackFileExporter()
	{
		ExportDirectory = PandoraPaths.OutputPath;
	}

	public bool Export(PackFile packFile)
	{
		//var launchDirectory = Environment.CurrentDirectory;

		var outputHandle = packFile.RebaseOutput(ExportDirectory);
		if (outputHandle.Directory == null) return false;
		if (!outputHandle.Directory.Exists) { outputHandle.Directory.Create(); }
		if (outputHandle.Exists) { outputHandle.Delete(); }
		HKXHeader header = HKXHeader.SkyrimSE();
		IHavokObject rootObject;

		try
		{
			using (var writeStream = outputHandle.Create())
			{
				var binaryWriter = new BinaryWriterEx(writeStream);
				var serializer = new PackFileSerializer();
				serializer.Serialize(packFile.Container, binaryWriter, header);
			}
		}
		catch (Exception ex)
		{
			Logger.Fatal($"Export > {packFile.ParentProject?.Identifier}~{packFile.Name} > FAILED > {ex}");
			using (var writeStream = outputHandle.Create())
			{
				var serializer = new HavokXmlSerializer();
				serializer.Serialize(packFile.Container, header, writeStream);
			}
			return false;
		}
		return true;
	}

	public PackFile Import(FileInfo file)
	{
		throw new NotImplementedException();
	}
	public void LoadMetaData()
	{
		if (!PreviousOutputFile.Exists)
		{
			Logger.Warn($"Previous output file not found");
			return;
		}

		try
		{
			using (FileStream readStream = PreviousOutputFile.OpenRead())
			{
				using (StreamReader reader = new(readStream))
				{
					string? expectedLine;
					while ((expectedLine = reader.ReadLine()) != null)
					{
						FileInfo file = new(expectedLine);
						if (!file.Exists) { continue; }

						file.Delete();

					}
				}
			}
		}
		catch (IOException ex)
		{
			Logger.Error(ex, $"I/O error while reading metadata file: {PreviousOutputFile.Name}");
		}
		catch (Exception ex)
		{
			Logger.Fatal(ex, $"Unexpected error while processing metadata file: {PreviousOutputFile.Name}");
			throw;
		}
	}

	public void SaveMetaData(IEnumerable<PackFile> packFiles)
	{
		PreviousOutputFile.Directory?.Create();

		using (FileStream readStream = PreviousOutputFile.Create())
		{
			using (StreamWriter writer = new(readStream))
			{
				foreach (PackFile packFile in packFiles)
				{
					if (!packFile.ExportSuccess) { continue; }

					writer.WriteLine(packFile.OutputHandle.FullName);
				}
			}
		}
	}
}

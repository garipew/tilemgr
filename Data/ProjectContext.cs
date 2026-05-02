using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

using Tilemgr;

namespace Data;

public class ProjectContext : DbContext
{
	public DbSet<Project> Projects { get; set; }

	public string DbPath { get; }

	public ProjectContext()
	{
		DbPath = "projects.db";
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Project>(e => 
		{
			e.OwnsOne(p => p.canvas);
			e.OwnsOne(p => p.palette);
		});
	}

	protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite($"Data Source={DbPath}");
}

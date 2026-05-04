using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

using Tilemgr;

namespace Data;

public class ProjectContext(DbContextOptions<ProjectContext> options) : DbContext(options)
{
	public DbSet<Project> Projects { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Project>(e => 
		{
			e.OwnsOne(p => p.canvas);
			e.OwnsOne(p => p.palette);
		});
		base.OnModelCreating(modelBuilder);
	}
}

﻿using Microsoft.EntityFrameworkCore;
using Persistence.Models.Entities;

namespace Persistence
{
	public class DocumentsDbContext : DbContext
	{
		public DocumentsDbContext(DbContextOptions<DocumentsDbContext> options) : base(options)
		{
		}

		// tables
		public DbSet<DocumentEntity> Documents { get; set; }
		public DbSet<UserEntity> Users { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<DocumentEntity>() //increment id
				.Property(d => d.Id)
				.ValueGeneratedOnAdd();
			
			base.OnModelCreating(modelBuilder);
		}
	}
}

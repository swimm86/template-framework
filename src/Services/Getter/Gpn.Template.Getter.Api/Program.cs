// ----------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Presentation.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddSharedPresentationCore();

var app = builder.Build();
app.ConfigurePresentationCore();

app.Run();

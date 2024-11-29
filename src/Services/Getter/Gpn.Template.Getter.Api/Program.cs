// ----------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="АО ИНЛАЙН ГРУП">
// Copyright (c) АО ИНЛАЙН ГРУП. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Application.Core.Configuration.Attributes;
using Shared.Presentation.Core.Extensions;

[assembly: EnvPath("./.env")]
var builder = WebApplication.CreateBuilder(args);
builder.AddSharedPresentationCore();

var app = builder.Build();
app.ConfigurePresentationCore();

app.Run();

// ----------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
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

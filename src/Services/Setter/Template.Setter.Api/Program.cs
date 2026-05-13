// ----------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Presentation.Core.Extensions;
using Template.Presentation.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.ImplementDependencies();

var app = builder.Build();
app.UseCommonPresentation();

app.Run();

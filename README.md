﻿# petssl_downloader
![Dotnet Build and Publish](https://github.com/recalde/petssl_downloader/workflows/Dotnet%20Build%20and%20Publish/badge.svg?branch=master)

## Overview
This project is used to download images and journal entries from petssl.com dog walking service.  It uses HtmlAgilityPack to parse HTML to find <img> tags, to download from aws s3, and parses journal html into a json file. Last the code uses CsvHelper nuget package to write summary reports of the journal data to make it more human readable.

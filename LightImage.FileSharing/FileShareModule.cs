﻿using Autofac;
using LightImage.FileSharing.Algorithms;
using LightImage.FileSharing.IO;
using LightImage.FileSharing.Managers;
using LightImage.FileSharing.Options;
using Microsoft.Extensions.Configuration;
using NetMQ;

namespace LightImage.FileSharing
{
    public class FileShareModule : Module
    {
        private readonly IConfiguration _config;

        public FileShareModule(IConfiguration config)
        {
            _config = config;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<FileShareShim>().AsSelf().SingleInstance();
            builder.RegisterType<FileShareService>().AsImplementedInterfaces().AsSelf().SingleInstance();
            builder.Configure<FileShareOptions>(_config, FileShareOptions.C_CONFIG_SECTION);
            builder.RegisterType<DownloadManager>().As<IDownloadManager>().SingleInstance();
            builder.RegisterType<UploadManager>().As<IUploadManager>().SingleInstance();
            builder.RegisterType<ChunkWriter>().As<IChunkWriter>().SingleInstance();
            builder.RegisterType<ChunkReader>().As<IChunkReader>().SingleInstance();
            builder.RegisterType<DownloadAlgorithm>().As<IDownloadAlgorithm>().SingleInstance();

            BufferPool.SetCustomBufferPool(new SystemBufferPool());
        }
    }
}
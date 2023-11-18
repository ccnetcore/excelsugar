﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ExcelSugar.Core;
using ExcelSugar.Core.Extensions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace ExcelSugar.Npoi
{
    public class NpoiOemExportable<T> : AbstractOemExportable<T>
    {
        public NpoiOemExportable(OemConfig oemConfig, IEnumerable<T>? expObj) : base(oemConfig, expObj)
        {
            _config = oemConfig;
            _expObjs = expObj;
        }

        public override Task<int> ExecuteCommandAsync()
        {
            if (base.LinkedListBuilder.FromContainer.Count != 0)
            {
                var fromPath = LinkedListBuilder.FromContainer.Last().Value;
                //没有传入实体，代表直接导出模板,及copy
                if (base._expObjs is null)
                {
                    ExportForNull(_config.Path, fromPath);
                }
                //代表存在模板
                this.ExportForTemplate(_expObjs.ToList(), _config.Path, fromPath);
            }
            else if (base._expObjs is not null)
            {
                this.ExportForAll(_expObjs.ToList(), _config.Path);
            }
            else
            {
                throw new Exception("未匹配到任何处理情况，暂不兼容");

            }
            return Task.FromResult(1);
        }

        private void ExportForNull(string filePath, string templatePath)
        {
            File.Copy(filePath, templatePath);
        }

        private void ExportForTemplate(List<T> entityList, string filePath, string templatePath)
        {
            var properties = typeof(T).GetValidProperties();
            //获取模板
            using (FileStream fileStream = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
            {
                // 创建工作簿
                IWorkbook workbook = new XSSFWorkbook(fileStream);


                var sheetName = typeof(T).GetSheetNameFromType();
                // 创建工作表
                ISheet sheet = workbook.GetSheet(sheetName);
                if (sheet is null)
                {
                    throw new Exception($"sheet 名称：【{sheetName}】 不存在");
                }

                // 写入表头
                IRow headerRow = sheet.GetOrCreateRow(0);
                for (int j = 0; j < properties.Count(); j++)
                {
                    headerRow.GetOrCreateCell(j).SetCellValue(properties[j].GetCustomAttribute<SugarHeadAttribute>()!.DisplayName);
                }

                // 写入数据
                for (int i = 0; i < entityList.Count(); i++)
                {
                    var currentEntity = entityList[i];
                    IRow dataRow = sheet.GetOrCreateRow(i + 1);
                    for (int j = 0; j < properties.Count(); j++)
                    {
                        var currentPropertiy = properties[j];
                        var cellStr = _config.CellValueConverter.PropertyToCell(currentPropertiy.GetValue(currentEntity));
                        //只处理简单类型
                        dataRow.GetOrCreateCell(j).SetCellValue(cellStr);
                    }
                }

                // 保存文件
                using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    workbook.Write(fs);
                }

                workbook.Dispose();
            }

        }


        /// <summary>
        /// 完整导出excel
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entityList"></param>
        /// <param name="filePath"></param>
        private void ExportForAll(List<T> entityList, string filePath)
        {
            var properties = typeof(T).GetValidProperties();
            // 创建工作簿
            IWorkbook workbook = new XSSFWorkbook();

            var sheetName = typeof(T).GetSheetNameFromType();
            // 创建工作表
            ISheet sheet = workbook.CreateSheet(sheetName);


            // 写入表头
            IRow headerRow = sheet.CreateRow(0);
            for (int j = 0; j < properties.Count(); j++)
            {
                headerRow.CreateCell(j).SetCellValue(properties[j].GetCustomAttribute<SugarHeadAttribute>()!.DisplayName);
            }

            // 写入数据
            for (int i = 0; i < entityList.Count(); i++)
            {
                var currentEntity = entityList[i];
                IRow dataRow = sheet.CreateRow(i + 1);
                for (int j = 0; j < properties.Count(); j++)
                {
                    var currentPropertiy = properties[j];
                    var cellStr = _config.CellValueConverter.PropertyToCell(currentPropertiy.GetValue(currentEntity));
                    //只处理简单类型
                    dataRow.CreateCell(j).SetCellValue(cellStr);
                }
            }

            // 保存文件
            using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }

            workbook.Dispose();
        }


    }
}

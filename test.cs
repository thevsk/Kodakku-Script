using System;
using KodakkuAssist.Module.GameEvent;
using KodakkuAssist.Script;
using KodakkuAssist.Module.GameEvent.Struct;
using KodakkuAssist.Module.Draw;
using KodakkuAssist.Data;
using KodakkuAssist.Module.Draw.Manager;
using KodakkuAssist.Module.GameOperate;
using KodakkuAssist.Module.GameEvent.Types;
using KodakkuAssist.Extensions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using Newtonsoft.Json;
using System.Linq;
using System.ComponentModel;
using Dalamud.Utility.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Vfx;
using Lumina.Excel.Sheets;
using System.Reflection;

namespace ThevskScript;

[ScriptType(
	name: "[0] 测试", 
	territorys: [],
    guid: "f47ac10b-58cc-4372-a567-0e02b2c3d479", 
	version: "0.0.0.1", 
	author: "Thevsk", 
	note: "test", 
	updateInfo: "none"
)]
public class KtisisHyperboreia
{

    public void Init(ScriptAccessory accessory)
    {
        accessory.Method.RemoveDraw(".*");
    }

	private static bool ParseObjectId(string? idStr, out uint id)
    {
        id = 0;
        if (string.IsNullOrEmpty(idStr)) return false;
        try
        {
            var idStr2 = idStr.Replace("0x", "");
            id = uint.Parse(idStr2, System.Globalization.NumberStyles.HexNumber);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
	
	[ScriptMethod(name: "Test", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:152"])]
    public void Test1(Event @event, ScriptAccessory accessory)
    {
		// 获得SourceId
		if (!ParseObjectId(@event["SourceId"], out var sid)) return;
		// 获得读条时长
		int.TryParse(@event["DurationMilliseconds"], out var dur);
			
        var dp = accessory.Data.GetDefaultDrawProperties();
		dp.Name = "Test";
		dp.Scale = new(5);
		dp.Owner = sid;
		dp.Color = accessory.Data.DefaultDangerColor;
		dp.Delay = 0;
		dp.DestoryAt = dur;
		accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
		InspectType(accessory.Method.GetType(), accessory);
    }
	
	private void InspectType(Type type, ScriptAccessory accessory)
    {
        try
        {
            var methods = type.GetMethods(
                BindingFlags.Public | 
                BindingFlags.Instance | 
                BindingFlags.Static
            );
            
            // 限制输出数量并优化格式
            foreach (var method in methods)
            {
                var parameters = string.Join(", ", method.GetParameters()
                    .Select(p => $"{p.ParameterType.Name} {p.Name}"));
                
                // 简化输出避免过长
                accessory.Method.SendChat($"/e {method.Name}({parameters})");
            }
        }
        catch (Exception ex)
        {
            accessory.Method.SendChat($"/e 类型探查出错: {ex.Message}");
        }
    }
}
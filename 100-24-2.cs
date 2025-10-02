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
	name: Name, 
	version: Version, 
	note: ChangeLog, 
	updateInfo: ChangeLog, 
    guid: "7b163f39-b48b-4b19-88bb-efa45bc8610f", 
	author: "Thevsk", 
	territorys: [1304]
)]
public class KtisisHyperboreia
{
	const string Version = "0.0.0.2";
	const string Name = "[100] [24] [团本] 桑多利亚：第二巡行";
	const string ChangeLog =
    """
        ChangeLog:
        [v0.0.0.1] 老1部分机制
        [v0.0.0.2] 完善老1机制（见过的都画了，打太快没见过后面的）
    """;

    [UserSetting("Debug模式")]
    public bool DebugMode { get; set; } = false;
	
	private uint Boss1ID = 0;
	private uint Boss1LeftHandID = 0;
	private uint Boss1RightHandID = 0;

    public void Init(ScriptAccessory accessory)
    {
		Boss1ID = 0;
		Boss1LeftHandID = 0;
		Boss1RightHandID = 0;
        accessory.Method.RemoveDraw(".*");
		if (DebugMode) accessory.Method.SendChat($"/e 已清空并释放所有资源");
		if (DebugMode) Boss1ID = 1073756683;
		if (DebugMode) Boss1LeftHandID = 1073756684;
		if (DebugMode) Boss1RightHandID = 1073756685;
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

	private float NormalizeRotation(float rotation)
	{
		const float twoPi = 2 * float.Pi;
		rotation %= twoPi;
		if (rotation < 0) rotation += twoPi;
		return rotation;
	}
	
	/// <summary>
	/// 计算从指定位置沿指定方向移动指定距离后的终点坐标
	/// </summary>
	/// <param name="startPos">起始位置</param>
	/// <param name="rotation">移动方向</param>
	/// <param name="distance">移动距离</param>
	/// <returns>终点位置</returns>
	private static Vector3 CalculateEndPosition(Vector3 startPos, float rotation, float distance)
	{
		// 计算方向向量
		Vector3 direction = new Vector3(
			MathF.Sin(rotation), 
			0, 
			MathF.Cos(rotation)
		);

		// 计算终点位置
		return startPos + direction * distance;
	}


	// boss-1-信仰之麒麟

	[ScriptMethod(name: "保存BOSSID", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:44433"], userControl: false)]
    public void SaveBoss1Id(Event @event, ScriptAccessory accessory)
    {
		// 获得SourceId
		if (!ParseObjectId(@event["SourceId"], out var sid)) return;
		Boss1ID = sid;
		if (DebugMode) accessory.Method.SendChat($"/e 记录Boss1ID: {Boss1ID}");
    }

	[ScriptMethod(name: "AOE", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:44490"])]
    public void Boss1Aoe(Event @event, ScriptAccessory accessory)
    {
		// 获得读条时长
		int.TryParse(@event["DurationMilliseconds"], out var dur);
		accessory.Method.TextInfo("AOE", dur, true);
    }

	[ScriptMethod(name: "死亡猛击", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(44444|44443)$"])]
    public void Boss1_swmj(Event @event, ScriptAccessory accessory)
    {
		// 获得SourceId
		if (!ParseObjectId(@event["SourceId"], out var sid)) return;
		// 获得读条时长
		int.TryParse(@event["DurationMilliseconds"], out var dur);
		// 获得目标朝向
		if(!float.TryParse(@event["SourceRotation"], out var rot)) return;
		// 开始绘制
		var dp = accessory.Data.GetDefaultDrawProperties();
		dp.Name = "死亡猛击";
		dp.Scale = new(32,60);
		dp.Offset = new(0,0,30);
		dp.Owner = sid;
		dp.Rotation = rot;
		dp.Color = accessory.Data.DefaultDangerColor;
		dp.Delay = 0;
		dp.DestoryAt = dur;

		accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
    }

	[ScriptMethod(name: "深红谜煞", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(45044|45045)$"])]
    public void Boss1_shms(Event @event, ScriptAccessory accessory)
    {
		// 获得SourceId
		if (!ParseObjectId(@event["SourceId"], out var sid)) return;
		// 获得读条时长
		int.TryParse(@event["DurationMilliseconds"], out var dur);
		// 判断施法方向
		float rotation = (@event.ActionId == 45045) ? float.Pi : 0f;
		// 开始绘制
		var dp = accessory.Data.GetDefaultDrawProperties();
		dp.Name = "深红谜煞";
		dp.Scale = new(30);
		dp.Radian = float.Pi;
		dp.Rotation = rotation;
		dp.FixRotation = false;
		dp.Owner = sid;
		dp.Color = accessory.Data.DefaultDangerColor;
		dp.DestoryAt = dur;
		accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
    }

	[ScriptMethod(name: "白虎", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(44431)$"])]
    public void Boss1_bh(Event @event, ScriptAccessory accessory)
    {
		// 获得SourceId
		if (!ParseObjectId(@event["SourceId"], out var sid)) return;
		// 获得读条时长
		int.TryParse(@event["DurationMilliseconds"], out var dur);
		// 获得目标朝向
		if(!float.TryParse(@event["SourceRotation"], out var rot)) return;
		// 获得目标位置
		var pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
		// 获得aoe2位置
		var aoe2pos = CalculateEndPosition(pos,rot,44);
		// 开始绘制
		var dp = accessory.Data.GetDefaultDrawProperties();
		dp.Name = "白虎";
		dp.Scale = new(12,50);
		dp.Owner = sid;
		dp.Color = accessory.Data.DefaultDangerColor;
		dp.Delay = 0;
		dp.DestoryAt = dur;

		accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
		
		dp.Name = "白虎-2";
		dp.Scale = new(20);
		dp.Position = aoe2pos;
		dp.Color = accessory.Data.DefaultDangerColor;
		dp.Delay = 0;
		dp.DestoryAt = dur + 1000;
		accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
    }

	[ScriptMethod(name: "死亡旋转", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(44440|44439)$"])]
    public void Boss1_swxz(Event @event, ScriptAccessory accessory)
    {
		// 获得SourceId
		if (!ParseObjectId(@event["SourceId"], out var sid)) return;
		// 检查BOSS实体
		if (Boss1ID == 0) return;
		// 获得读条时长
		int.TryParse(@event["DurationMilliseconds"], out var dur);
		// 开始绘制
		var dp = accessory.Data.GetDefaultDrawProperties();
		dp.Name = "死亡旋转";
		dp.Scale = new(30);
		dp.InnerScale = new(14);
		dp.Radian = float.Pi * 2;
		dp.Owner = Boss1ID;
		dp.Color = accessory.Data.DefaultDangerColor;
		dp.Delay = 0;
		dp.DestoryAt = dur;

		accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Donut, dp);
		
		if (@event.ActionId == 44440)
		{
			Boss1LeftHandID = sid;
			if (DebugMode) accessory.Method.SendChat($"/e 记录左手ID: {Boss1LeftHandID}");
		}
		else
		{
			Boss1RightHandID = sid;
			if (DebugMode) accessory.Method.SendChat($"/e 记录右手ID: {Boss1RightHandID}");
		}
    }

	[ScriptMethod(name: "左右手猛击", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(44435|44452)$"])]
    public void Boss1_zysswmj(Event @event, ScriptAccessory accessory)
    {
		// 获取aid
		var aid = @event.ActionId;
		// 获得读条时长
		int.TryParse(@event["DurationMilliseconds"], out var dur);
		// 匹配手臂
		var sid = (aid == 44435) ? Boss1RightHandID : Boss1LeftHandID;
		if (sid == 0) return;
		// 开始绘制
		var dp = accessory.Data.GetDefaultDrawProperties();
		dp.Name = "左/右手猛击";
		dp.Scale = new(30);
		dp.Owner = sid;
		dp.Color = accessory.Data.DefaultDangerColor;
		dp.Delay = 0;
		dp.DestoryAt = dur + 4700;

		accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
    }

	[ScriptMethod(name: "旋转连击", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^(44445)$"])]
    public void Boss1_xzlj(Event @event, ScriptAccessory accessory)
    {
		// 获得SourceId
		if (!ParseObjectId(@event["SourceId"], out var sid)) return;
		// 获得读条时长
		int.TryParse(@event["DurationMilliseconds"], out var dur);
		// 延迟
		var delay = 5000;
		dur = dur - delay;
		// 开始绘制
		var dp = accessory.Data.GetDefaultDrawProperties();
		dp.Name = "旋转连击";
		dp.Scale = new(14);
		dp.Owner = sid;
		dp.Color = accessory.Data.DefaultDangerColor;
		dp.Delay = delay;
		dp.DestoryAt = dur;

		accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
    }

	[ScriptMethod(name: "3T踩塔", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:44470"])]
    public void Boss1_3tct(Event @event, ScriptAccessory accessory)
    {
		// 获得读条时长
		int.TryParse(@event["DurationMilliseconds"], out var dur);
		accessory.Method.TextInfo("3T踩塔", dur, true);
    }
}
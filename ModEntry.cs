using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace autowine
{
    /// <summary>模组入口点</summary>
    public class ModEntry : Mod
    {
        /*********
        ** 公共方法
        *********/
        /// <summary>模组的入口点，在首次加载模组后自动调用</summary>
        /// <param name="helper">对象 helper 提供用于编写模组的简化接口</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            //意思是将 OnButtonPressed 方法绑定到 SMAPI 的 ButtonPressed 按钮按下事件
            //this 表示本对象，也就是当前的 ModEntry 类
        }

        /*********
        ** 私有方法
        *********/
        /// <summary>在玩家按下键盘、控制器或鼠标上的按钮后引发</summary>
        /// <param name="sender">对象 sender 表示调用此方法的对象</param>
        /// <param name="e">对象 e 表示事件数据</param>
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            if (e.Button == SButton.MouseRight)
            {
                Vector2 cursorTile = e.Cursor.GrabTile;
                GameLocation location = Game1.currentLocation;
                if (location == null)
                    return;
                if (location.objects.TryGetValue(cursorTile, out StardewValley.Object obj))
                {
                    if (obj.Name == "Keg")
                    {
                        if (obj.readyForHarvest.Value)
                        {
                            Game1.showGlobalMessage("小桶可以收获了");
                            HarvestAllReadyKegs();
                        }
                        else
                        {
                            if (obj.heldObject.Value != null)
                            {
                                Game1.showGlobalMessage("小桶已经装满了");
                            }
                            TryFillKegs();
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }
        private void TryFillKegs()
        {
            int sum=0;
            foreach(var item in Game1.player.Items)
            {
                if (item != null && item is StardewValley.Object obj)
                {
                    if(item.Name=="Ancient Fruit"||item.Name=="StarFruit"||item.Name=="Hop")
                    {
                        sum+=FindKegsAndFillThem(item,Game1.currentLocation);
                    }
                }
            }
            Game1.showGlobalMessage($"填充了 {sum} 个酒桶！");
        }

        private int FindKegsAndFillThem(Item item, GameLocation currentLocation)
        {
            int sum=item.Stack; //物品的数量
            int filledNum=0;
            foreach (var pair in currentLocation.objects.Pairs)
            {
                StardewValley.Object obj = pair.Value;
                if (obj.Name == "Keg" && obj.heldObject.Value == null)
                {
                    obj.performObjectDropInAction(item, false, Game1.player);
                    sum--;
                    filledNum++;
                    if (sum == 0){
                        Game1.player.removeItemFromInventory(item);
                        return filledNum;
                    }
                    
                }
            }
            if(sum!=0){//如果还有剩余的物品,且当前场景是农场,则遍历农场内的建筑,找到酒桶
                if(currentLocation is Farm farm){
                    foreach (var building in farm.buildings)
                    {
                        GameLocation indoors = building.indoors.Value;
                        if (indoors != null)
                        {
                            foreach (var pair in indoors.objects.Pairs)
                            {
                                StardewValley.Object obj = pair.Value;
                                if (obj.Name == "Keg" && obj.heldObject.Value == null)
                                {
                                    obj.performObjectDropInAction(item, false, Game1.player);
                                    filledNum++;
                                    sum--;
                                    if (sum == 0){
                                        Game1.player.removeItemFromInventory(item);
                                        return filledNum;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if(sum!=0){//如果还有剩余的物品,则遍历所有场景,找到酒桶
                foreach (GameLocation location in Game1.locations)
                {
                    if(location!=currentLocation){
                        foreach (var pair in location.objects.Pairs)
                        {
                            StardewValley.Object obj = pair.Value;
                            if (obj.Name == "Keg" && obj.heldObject.Value == null)
                            {
                                obj.performObjectDropInAction(item, false, Game1.player);
                                filledNum++;
                                sum--;
                                if (sum == 0){
                                    Game1.player.removeItemFromInventory(item);
                                    return filledNum;
                                }
                            }
                        }
                    }
                }
            }
            
            return filledNum;
        }

        private void HarvestAllReadyKegs()
        {
            int count = 0;

            // 遍历所有场景
            foreach (GameLocation location in Game1.locations)
            {
                // 收获当前场景的酒桶
                count += HarvestKegsInLocation(location);

                // 如果是农场，遍历所有建筑
                if (location is Farm farm)
                {
                    foreach (var building in farm.buildings)
                    {
                        // 获取建筑内部的Location
                        GameLocation indoors = building.indoors.Value;
                        if (indoors != null)
                        {
                            // 收获建筑内的酒桶
                            count += HarvestKegsInLocation(indoors);
                        }
                    }
                }
            }

            if (count > 0)
            {
                Game1.showGlobalMessage($"你收获了 {count} 个酒桶！");
            }
            else
            {
                Game1.showGlobalMessage("没有可收获的酒桶。");
            }
        }

        // 新增方法：收获某个Location中的酒桶
        private int HarvestKegsInLocation(GameLocation location)
        {
            int count = 0;

            foreach (var pair in location.objects.Pairs)
            {
                StardewValley.Object obj = pair.Value;

                // 判断是否为酒桶，并且是否已经可以收获
                if (obj.Name == "Keg" && obj.readyForHarvest.Value)
                {
                    // 获取酒桶产出的物品
                    Item harvestedItem = obj.heldObject.Value;

                    // 尝试将物品添加到玩家背包
                    if (Game1.player.addItemToInventoryBool(harvestedItem))
                    {
                        // 清空酒桶
                        obj.heldObject.Value = null;
                        obj.readyForHarvest.Value = false;
                        count++;
                    }
                }
            }

            return count;
        }

    }
}
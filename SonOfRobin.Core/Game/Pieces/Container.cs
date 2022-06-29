﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;


namespace SonOfRobin
{
    public class Container : BoardPiece
    {
        public Container(World world, Vector2 position, AnimPkg animPackage, PieceTemplate.Name name, AllowedFields allowedFields, Dictionary<byte, int> maxMassBySize, byte storageWidth, byte storageHeight, string readableName, string description, Category category,
            byte animSize = 0, string animName = "open", bool blocksMovement = true, ushort minDistance = 0, ushort maxDistance = 100, int destructionDelay = 0, bool floatsOnWater = false, int generation = 0, Yield yield = null, int maxHitPoints = 1, bool fadeInAnim = false) :

            base(world: world, position: position, animPackage: animPackage, animSize: animSize, animName: animName, blocksMovement: blocksMovement, minDistance: minDistance, maxDistance: maxDistance, name: name, destructionDelay: destructionDelay, allowedFields: allowedFields, floatsOnWater: floatsOnWater, maxMassBySize: maxMassBySize, generation: generation, canBePickedUp: false, yield: yield, maxHitPoints: maxHitPoints, fadeInAnim: fadeInAnim, isShownOnMiniMap: true, readableName: readableName, description: description, category: category)
        {
            this.activeState = State.Empty;
            this.boardTask = Scheduler.TaskName.OpenContainer;
            this.pieceStorage = new PieceStorage(width: storageWidth, height: storageHeight, world: this.world, storagePiece: this, storageType: PieceStorage.StorageType.Chest);
        }

        public override Dictionary<string, Object> Serialize()
        {
            Dictionary<string, Object> pieceData = base.Serialize();
            // data to serialize here
            return pieceData;
        }

        public override void Deserialize(Dictionary<string, Object> pieceData)
        {
            base.Deserialize(pieceData);
            // data to deserialize here
        }

        public void Open()
        {
            if (this.sprite.animName == "open" || this.sprite.animName == "opening") return;
            this.sprite.AssignNewName(animName: "opening");
        }

        public void Close()
        {
            if (this.pieceStorage.OccupiedSlots.Count == 0 || this.sprite.animName == "closing") return;
            this.sprite.AssignNewName(animName: "closing");
        }


    }
}
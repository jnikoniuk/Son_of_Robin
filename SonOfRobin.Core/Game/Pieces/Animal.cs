﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;


namespace SonOfRobin
{
    public class Animal : BoardPiece
    {
        public static readonly int maxAnimalsPerName = 45; // 45

        private readonly bool female;
        private readonly int maxMass;
        private readonly float massBurnedMultiplier;
        private readonly byte awareness;
        private readonly int matureAge;
        private readonly uint pregnancyDuration;
        private readonly byte maxChildren;
        private readonly int strength;
        private uint pregnancyMass;
        private int attackCooldown;
        private readonly int maxFedLevel;
        private int fedLevel;
        private readonly float maxStamina;
        private float stamina;
        private readonly ushort sightRange;
        public AiData aiData;
        public BoardPiece target;
        private readonly List<PieceTemplate.Name> eats;
        private readonly List<PieceTemplate.Name> isEatenBy;

        private float FedPercentage // float 0-1
        { get { return (float)this.fedLevel / (float)maxFedLevel; } }

        private float RealSpeed
        { get { return stamina > 0 ? this.speed : Math.Max(this.speed / 2, 1); } }

        public float MaxMassPercentage { get { return this.Mass / this.maxMass; } }

        public Animal(World world, Vector2 position, AnimPkg animPackage, PieceTemplate.Name name, AllowedFields allowedFields, Dictionary<byte, int> maxMassBySize, int mass, int maxMass, byte awareness, bool female, int maxAge, int matureAge, uint pregnancyDuration, byte maxChildren, float maxStamina, int maxHitPoints, ushort sightRange, List<PieceTemplate.Name> eats, List<PieceTemplate.Name> isEatenBy, int strength, float massBurnedMultiplier, byte animSize = 0, string animName = "default", float speed = 1, bool blocksMovement = true, ushort minDistance = 0, ushort maxDistance = 100, int destructionDelay = 0, bool floatsOnWater = false, int generation = 0, Yield yield = null, bool fadeInAnim = true) :

            base(world: world, position: position, animPackage: animPackage, mass: mass, animSize: animSize, animName: animName, blocksMovement: blocksMovement, minDistance: minDistance, maxDistance: maxDistance, name: name, destructionDelay: destructionDelay, allowedFields: allowedFields, floatsOnWater: floatsOnWater, maxMassBySize: maxMassBySize, generation: generation, speed: speed, maxAge: maxAge, maxHitPoints: maxHitPoints, yield: yield, fadeInAnim: fadeInAnim, isShownOnMiniMap: true)
        {
            this.activeState = State.AnimalAssessSituation;
            this.target = null;
            this.maxMass = maxMass;
            this.massBurnedMultiplier = massBurnedMultiplier;
            this.awareness = awareness;
            this.female = female;
            this.matureAge = matureAge;
            this.pregnancyDuration = pregnancyDuration;
            this.pregnancyMass = 0;
            this.maxChildren = maxChildren;
            this.attackCooldown = 0; // earliest world.currentUpdate, when attacking will be possible
            this.maxFedLevel = 1000;
            this.fedLevel = maxFedLevel;
            this.maxStamina = maxStamina;
            this.stamina = maxStamina;
            this.sightRange = sightRange;
            this.eats = eats;
            this.isEatenBy = isEatenBy;
            this.aiData = new AiData();
            this.strength = strength;

            new WorldEvent(eventName: WorldEvent.EventName.Death, world: this.world, delay: this.maxAge, boardPiece: this);
        }

        public override Dictionary<string, Object> Serialize()
        {
            Dictionary<string, Object> pieceData = base.Serialize();

            pieceData["animal_female"] = this.female; // used in PieceTemplate, to create animal of correct sex
            pieceData["animal_attackCooldown"] = this.attackCooldown;
            pieceData["animal_fedLevel"] = this.fedLevel;
            pieceData["animal_pregnancyMass"] = this.pregnancyMass;
            pieceData["animal_aiData"] = this.aiData;
            pieceData["animal_target_old_id"] = this.target?.id;

            return pieceData;
        }

        public override void Deserialize(Dictionary<string, Object> pieceData)
        {
            base.Deserialize(pieceData);

            this.attackCooldown = (int)pieceData["animal_attackCooldown"];
            this.fedLevel = (int)pieceData["animal_fedLevel"];
            this.pregnancyMass = (uint)pieceData["animal_pregnancyMass"];
            this.aiData = (AiData)pieceData["animal_aiData"];
            this.aiData.UpdatePosition(); // needed to update Vector2
        }

        public override void DrawStatBar()
        {
            if (!this.alive) return;

            int posX = this.sprite.gfxRect.Center.X;
            int posY = this.sprite.gfxRect.Bottom;

            new StatBar(label: "hp", value: (int)this.hitPoints, valueMax: (int)this.maxHitPoints, colorMin: new Color(255, 0, 0), colorMax: new Color(0, 255, 0), posX: posX, posY: posY);

            if (Preferences.debugShowStatBars)
            {
                new StatBar(label: "stam", value: (int)this.stamina, valueMax: (int)this.maxStamina, colorMin: new Color(100, 100, 100), colorMax: new Color(255, 255, 255), posX: posX, posY: posY);
                new StatBar(label: "food", value: (int)this.fedLevel, valueMax: (int)this.maxFedLevel, colorMin: new Color(0, 128, 255), colorMax: new Color(0, 255, 255), posX: posX, posY: posY);
                new StatBar(label: "age", value: (int)this.currentAge, valueMax: (int)this.maxAge, colorMin: new Color(180, 0, 0), colorMax: new Color(255, 0, 0), posX: posX, posY: posY);
                new StatBar(label: "weight", value: (int)this.Mass, valueMax: (int)this.maxMass, colorMin: new Color(0, 128, 255), colorMax: new Color(0, 255, 255), posX: posX, posY: posY);
            }

            StatBar.FinishThisBatch();
        }

        public void ExpendEnergy(float energyAmount)
        {
            energyAmount *= massBurnedMultiplier;

            if (this.fedLevel > 0)
            {
                this.fedLevel = Convert.ToInt16(Math.Max(this.fedLevel - Math.Max(energyAmount / 2, 1), 0));
            }
            else // burning "fat"
            {
                if (this.Mass >= this.startingMass * 2)
                { this.Mass = Math.Max(this.Mass - energyAmount, this.startingMass); }
                else
                { this.hitPoints = Math.Max(this.hitPoints - 0.05f, 0); }
            }

            this.stamina = Math.Max(this.stamina - Math.Max((energyAmount / 2), 1), 0);
        }

        public void AcquireEnergy(float energyAmount)
        {
            energyAmount *= this.efficiency;
            int massGained = Math.Max(Convert.ToInt32(energyAmount / 4), 1);

            this.hitPoints = Math.Min(this.hitPoints + (energyAmount / 3), this.maxHitPoints);
            this.fedLevel = Math.Min(this.fedLevel + Convert.ToInt16(energyAmount * 2), this.maxFedLevel);
            this.stamina = Math.Min(this.stamina + 1, this.maxStamina);

            if (this.pregnancyMass > 0 && this.pregnancyMass < this.startingMass * this.maxChildren)
            { this.pregnancyMass += (uint)massGained; }
            else
            { this.Mass = Math.Min(this.Mass + massGained, this.maxMass); }
        }

        public List<BoardPiece> AssessAsMatingPartners(List<BoardPiece> pieces)
        {
            var sameSpecies = pieces.Where(piece =>
                piece.GetType() == typeof(Animal) &&
                piece.alive &&
                piece.name == this.name
                ).ToList();

            List<Animal> matingPartners = sameSpecies.Cast<Animal>().ToList();

            matingPartners = matingPartners.Where(animal =>
            animal.female != this.female &&
            animal.pregnancyMass == 0 &&
            animal.currentAge >= animal.matureAge
            ).ToList();

            return matingPartners.Cast<BoardPiece>().ToList();
        }

        public List<BoardPiece> GetSeenPieces()
        { return world.grid.GetPiecesWithinDistance(groupName: Cell.Group.ColAll, mainSprite: this.sprite, distance: this.sightRange); }

        public override void SM_AnimalAssessSituation()
        {
            this.target = null;
            this.sprite.CharacterStand();

            if (this.world.random.Next(0, 30) == 0) // to avoid getting blocked
            {
                this.activeState = State.AnimalWalkAround;
                this.aiData.Reset();
                this.aiData.SetTimeLeft(700);
                return;
            }

            // looking around

            List<BoardPiece> seenPieces = this.GetSeenPieces();

            if (seenPieces.Count == 0)
            {
                this.activeState = State.AnimalWalkAround;
                this.aiData.Reset();
                this.aiData.SetTimeLeft(16000);
                this.aiData.SetDontStop(true);
                return;
            }

            // looking for enemies

            float enemyDistance = 10000000;
            BoardPiece enemyPiece = null;

            var enemyList = seenPieces.Where(piece => this.isEatenBy.Contains(piece.name)).ToList();
            if (enemyList.Count > 0)
            {
                enemyPiece = BoardPiece.FindClosestPiece(sprite: this.sprite, pieceList: enemyList);
                enemyDistance = Vector2.Distance(this.sprite.position, enemyPiece.sprite.position);
            }

            // looking for food

            var foodList = seenPieces.Where(piece => this.eats.Contains(piece.name) && piece.exists && piece.Mass > 0 && this.sprite.allowedFields.CanStandHere(position: piece.sprite.position)).ToList();

            BoardPiece foodPiece = null;

            if (foodList.Count > 0)
            {
                var deadFoodList = foodList.Where(piece => !piece.alive).ToList();
                if (deadFoodList.Count > 0) foodList = deadFoodList;
                foodPiece = this.world.random.Next(0, 8) != 0 ? foodList[0] : foodList[world.random.Next(0, foodList.Count)];
            }

            // looking for mating partner

            BoardPiece matingPartner = null;
            if (this.currentAge >= matureAge && this.pregnancyMass == 0 && enemyPiece == null)
            {
                List<BoardPiece> matingPartners = this.AssessAsMatingPartners(seenPieces);
                if (matingPartners.Count > 0)
                {
                    if (this.world.random.Next(0, 8) != 0)
                    { matingPartner = BoardPiece.FindClosestPiece(sprite: this.sprite, pieceList: matingPartners); }
                    else
                    { matingPartner = matingPartners[world.random.Next(0, matingPartners.Count)]; }

                }
            }

            // choosing what to do

            DecisionEngine decisionEngine = new DecisionEngine();
            decisionEngine.AddChoice(action: DecisionEngine.Action.Flee, piece: enemyPiece, priority: 1.2f - ((float)enemyDistance / this.sightRange));
            decisionEngine.AddChoice(action: DecisionEngine.Action.Eat, piece: foodPiece, priority: (this.hitPoints / this.maxHitPoints) > 0.3f ? 1.2f - this.FedPercentage : 1.5f);
            decisionEngine.AddChoice(action: DecisionEngine.Action.Mate, piece: matingPartner, priority: 1f);
            var bestChoice = decisionEngine.GetBestChoice();

            if (bestChoice is null)
            {
                this.activeState = State.AnimalWalkAround;
                this.aiData.Reset();
                this.aiData.SetTimeLeft(10000);
                this.aiData.SetDontStop(true);
                return;
            }

            this.aiData.Reset();
            this.target = bestChoice.piece;

            switch (bestChoice.action)
            {
                case DecisionEngine.Action.Flee:
                    this.activeState = State.AnimalFlee;
                    break;

                case DecisionEngine.Action.Eat:
                    if (this.target.GetType() == typeof(Animal)) // target animal has a chance to flee early
                    {
                        Animal animalTarget = (Animal)this.target;
                        if (this.world.random.Next(0, Convert.ToInt32(animalTarget.awareness / 3)) == 0)
                        {
                            animalTarget.target = this;
                            animalTarget.aiData.Reset();
                            animalTarget.activeState = State.AnimalFlee;
                        }
                    }

                    this.activeState = State.AnimalChaseTarget;
                    break;

                case DecisionEngine.Action.Mate:
                    this.activeState = State.AnimalChaseTarget;
                    break;

                default:
                    throw new DivideByZeroException($"Unsupported choice action - {bestChoice.action}.");
            }
        }

        public override void SM_AnimalWalkAround()
        {
            if (this.aiData.timeLeft <= 0)
            {
                this.activeState = State.AnimalAssessSituation;
                this.aiData.Reset();
                return;
            }

            this.aiData.timeLeft--;

            if (this.stamina < 20)
            {
                this.activeState = State.AnimalRest;
                this.aiData.Reset();
                return;
            }

            if (this.aiData.Coordinates == null)
            {
                while (true)
                {
                    var coordinates = new List<int> {
                        Math.Min(Math.Max((int)this.sprite.position.X + this.world.random.Next(-2000, 2000), 0), this.world.width - 1),
                        Math.Min(Math.Max((int)this.sprite.position.Y + this.world.random.Next(-2000, 2000), 0), this.world.height - 1)
                    };
                    if (this.sprite.allowedFields.CanStandHere(position: new Vector2(coordinates[0], coordinates[1])))
                    {
                        this.aiData.SetCoordinates(coordinates);
                        break;
                    }
                }
            }

            bool successfullWalking = this.GoOneStepTowardsGoal(goalPosition: this.aiData.Position, splitXY: false, walkSpeed: Math.Max(this.speed / 2, 1));
            this.ExpendEnergy(Math.Max(this.RealSpeed / 6, 1));

            if (successfullWalking && Vector2.Distance(this.sprite.position, this.aiData.Position) < 10)
            {
                this.activeState = State.AnimalAssessSituation;
                this.aiData.Reset();
                return;
            }

            // after some walking, it would be a good idea to stop and look around

            if (!successfullWalking || !this.aiData.dontStop && (this.world.random.Next(0, this.awareness * 2) == 0))
            {
                this.activeState = State.AnimalAssessSituation;
                this.aiData.Reset();
                return;
            }

        }

        public override void SM_AnimalRest()
        {
            if (this.world.currentUpdate > this.aiData.sleepShownUpdate + 60)
            {
                BoardPiece zzz = PieceTemplate.CreateOnBoard(world: this.world, position: this.sprite.position, templateName: PieceTemplate.Name.Zzz);
                new Tracking(world: world, targetSprite: this.sprite, followingSprite: zzz.sprite);
                this.aiData.SetSleepShown(this.world.currentUpdate);
            }

            this.target = null;
            this.sprite.CharacterStand();

            this.stamina = Math.Min(this.stamina + 3, this.maxStamina);
            if (this.stamina == this.maxStamina || this.world.random.Next(0, this.awareness * 10) == 0)
            {
                this.activeState = State.AnimalAssessSituation;
                this.aiData.Reset();
            }
        }

        public override void SM_AnimalChaseTarget()
        {
            if (this.target == null || !this.target.exists || Vector2.Distance(this.sprite.position, this.target.sprite.position) > this.sightRange)
            {
                this.activeState = State.AnimalAssessSituation;
                this.aiData.Reset();
                return;
            }

            if (this.sprite.CheckIfOtherSpriteIsWithinRange(target: target.sprite, range: this.target.GetType() == typeof(Plant) ? 4 : 10))
            {
                if (this.eats.Contains(this.target.name))
                {
                    this.activeState = State.AnimalAttack;
                    this.aiData.Reset();
                    return;
                }

                else if (this.AssessAsMatingPartners(new List<BoardPiece> { this.target }) != null)
                {
                    this.activeState = State.AnimalMate;
                    this.aiData.Reset();
                    return;
                }

                else throw new DivideByZeroException($"Target is not food nor mate.");
            }

            if (this.world.random.Next(0, this.awareness) == 0) // once in a while it is good to look around and assess situation
            {
                this.activeState = State.AnimalAssessSituation;
                this.aiData.Reset();
                return;
            }

            bool successfullWalking = this.GoOneStepTowardsGoal(goalPosition: this.target.sprite.position, splitXY: false, walkSpeed: this.RealSpeed);

            if (successfullWalking)
            {
                this.ExpendEnergy(Convert.ToInt32(Math.Max(this.RealSpeed / 2, 1)));
                if (this.stamina <= 0)
                {
                    this.activeState = State.AnimalRest;
                    this.aiData.Reset();
                    return;
                }
            }
            else
            {
                this.activeState = State.AnimalWalkAround;
                this.aiData.Reset();
                this.aiData.SetTimeLeft(1000);
            }
        }

        public override void SM_AnimalAttack()
        {
            this.sprite.CharacterStand();

            if (this.target is null ||
                !this.target.exists ||
                this.target.Mass <= 0 ||
                !this.sprite.CheckIfOtherSpriteIsWithinRange(target: this.target.sprite, range: this.target.GetType() == typeof(Plant) ? 4 : 15))
            {
                this.activeState = State.AnimalAssessSituation;
                this.aiData.Reset();
                return;
            }

            if (!this.target.alive || this.target.GetType() == typeof(Plant))
            {
                this.activeState = State.AnimalEat;
                this.aiData.Reset();
                return;
            }

            if (this.target.GetType() == typeof(Animal))
            {
                Animal animalTarget = (Animal)this.target;
                animalTarget.target = this;
                animalTarget.aiData.Reset();
                animalTarget.activeState = State.AnimalFlee;

                if (this.world.currentUpdate < this.attackCooldown) return;
                this.attackCooldown = this.world.currentUpdate + 20;

                int attackChance = Convert.ToInt32(Math.Max(Math.Min((float)animalTarget.RealSpeed / (float)this.RealSpeed, 30), 1)); // 1 == guaranteed hit, higher values == lower chance
                if (this.world.random.Next(0, attackChance) == 0)
                {
                    BoardPiece attackEffect = PieceTemplate.CreateOnBoard(world: this.world, position: animalTarget.sprite.position, templateName: PieceTemplate.Name.Attack);
                    new Tracking(world: world, targetSprite: animalTarget.sprite, followingSprite: attackEffect.sprite);

                    if (this.world.random.Next(0, 2) == 0) PieceTemplate.CreateOnBoard(world: this.world, position: animalTarget.sprite.position, templateName: PieceTemplate.Name.BloodSplatter);

                    if (animalTarget.yield != null) animalTarget.yield.DropDebris();

                    int attackStrength = Convert.ToInt32(this.world.random.Next(Convert.ToInt32(this.strength * 0.75), Convert.ToInt32(this.strength * 1.5)) * this.efficiency);
                    animalTarget.hitPoints = Math.Max(0, animalTarget.hitPoints - attackStrength);
                    if (animalTarget.hitPoints <= 0) animalTarget.Kill();

                    animalTarget.AddPassiveMovement(movement: (this.sprite.position - animalTarget.sprite.position) * -0.3f * attackStrength);

                }

                else // if attack has missed
                {
                    BoardPiece miss = PieceTemplate.CreateOnBoard(world: this.world, position: animalTarget.sprite.position, templateName: PieceTemplate.Name.Miss);
                    new Tracking(world: world, targetSprite: animalTarget.sprite, followingSprite: miss.sprite);
                }
            }
        }

        public override void SM_AnimalEat()
        {
            this.sprite.CharacterStand();

            if (this.target == null || !this.target.exists || this.target.Mass <= 0)
            {
                this.activeState = State.AnimalAssessSituation;
                this.aiData.Reset();
                return;
            }

            bool eatingPlant = this.target.GetType() == typeof(Plant);  // meat is more nutricious than plants
            var bittenMass = Math.Min(eatingPlant ? 25 : 5, this.target.Mass);
            this.AcquireEnergy(bittenMass * (eatingPlant ? 0.5f : 6f));

            this.target.Mass = Math.Max(this.target.Mass - bittenMass, 0);

            if (this.target.Mass <= 0)
            {
                this.target.Destroy();
                this.activeState = State.AnimalAssessSituation;
                this.aiData.Reset();
                return;
            }

            if ((this.Mass >= this.maxMass && this.pregnancyMass == 0) || this.world.random.Next(0, this.awareness) == 0)
            {
                this.activeState = State.AnimalAssessSituation;
                this.aiData.Reset();
                return;
            }
        }

        public override void SM_AnimalMate()
        {
            this.sprite.CharacterStand();

            if (this.target == null)
            {
                this.activeState = State.AnimalAssessSituation;
                this.aiData.Reset();
                return;
            }

            Animal animalMate = (Animal)this.target;

            if (!(this.pregnancyMass == 0 && animalMate.pregnancyMass == 0 && this.target.alive && this.sprite.CheckIfOtherSpriteIsWithinRange(animalMate.sprite, range: 15)))
            {
                this.activeState = State.AnimalAssessSituation;
                this.aiData.Reset();
                return;
            }

            var heart1 = PieceTemplate.CreateOnBoard(world: world, position: this.sprite.position, templateName: PieceTemplate.Name.Heart);
            var heart2 = PieceTemplate.CreateOnBoard(world: world, position: animalMate.sprite.position, templateName: PieceTemplate.Name.Heart);
            new Tracking(world: world, targetSprite: this.sprite, followingSprite: heart1.sprite);
            new Tracking(world: world, targetSprite: animalMate.sprite, followingSprite: heart2.sprite);

            Animal female = this.female ? this : animalMate;
            female.pregnancyMass = 1; // starting mass should be greater than 0

            new WorldEvent(world: this.world, delay: (int)female.pregnancyDuration, boardPiece: female, eventName: WorldEvent.EventName.Birth);

            this.stamina = 0;
            animalMate.stamina = 0;

            this.activeState = State.AnimalRest;
            this.aiData.Reset();

            animalMate.activeState = State.AnimalRest;
            animalMate.aiData.Reset();
        }

        public override void SM_AnimalGiveBirth()
        {
            this.sprite.CharacterStand();

            // excess fat cam be converted to pregnancy
            if (this.Mass > this.startingMass * 2 && this.pregnancyMass < this.startingMass * this.maxChildren)
            {
                var missingMass = (this.startingMass * this.maxChildren) - this.pregnancyMass;
                var convertedFat = Convert.ToInt32(Math.Floor(Math.Min(this.Mass - (this.startingMass * 2), missingMass)));
                this.Mass -= convertedFat;
                this.pregnancyMass += (uint)convertedFat;
            }

            uint noOfChildren = Convert.ToUInt32(Math.Min(Math.Floor(this.pregnancyMass / this.startingMass), this.maxChildren));
            uint childrenBorn = 0;

            for (int i = 0; i < noOfChildren; i++)
            {
                if (this.world.pieceCountByName[this.name] >= maxAnimalsPerName)
                // to avoid processing too many animals, which are heavy on CPU
                {
                    var fat = this.pregnancyMass;
                    this.Mass = Math.Min(this.Mass + fat, this.maxMass);
                    this.pregnancyMass = 0;

                    this.activeState = State.AnimalAssessSituation;
                    this.aiData.Reset();
                    return;
                }

                BoardPiece child = PieceTemplate.CreateOnBoard(world: world, position: this.sprite.position, templateName: this.name, generation: this.generation + 1);
                if (child.sprite.placedCorrectly)
                {
                    childrenBorn++;
                    this.pregnancyMass -= (uint)this.startingMass;

                    var backlight = PieceTemplate.CreateOnBoard(world: world, position: child.sprite.position, templateName: PieceTemplate.Name.Backlight);
                    new Tracking(world: world, targetSprite: child.sprite, followingSprite: backlight.sprite, targetXAlign: XAlign.Center, targetYAlign: YAlign.Center);
                }
            }

            if (childrenBorn > 0) MessageLog.AddMessage(currentFrame: SonOfRobinGame.currentUpdate, msgType: MsgType.Debug, message: $"{this.name} has been born ({childrenBorn}).");

            if (this.pregnancyMass > this.startingMass)
            { new WorldEvent(world: this.world, delay: 90, boardPiece: this, eventName: WorldEvent.EventName.Birth); }
            else
            {
                this.Mass = Math.Min(this.Mass + this.pregnancyMass, this.maxMass);
                this.pregnancyMass = 0;
            }

            this.activeState = State.AnimalRest;
            this.aiData.Reset();
        }

        public override void SM_AnimalFlee()
        {
            if (this.stamina == 0)
            {
                this.activeState = State.AnimalRest;
                this.aiData.Reset();
                return;
            }

            if (this.target == null || !this.target.alive)
            {
                this.activeState = State.AnimalAssessSituation;
                this.aiData.Reset();
                return;
            }

            // adrenaline raises maximum speed without using more energy than normal
            bool successfullRunningAway = this.GoOneStepTowardsGoal(goalPosition: this.target.sprite.position, splitXY: false, walkSpeed: Math.Max(this.speed * 1.2f, 1), runFrom: true);

            if (successfullRunningAway)
            {
                this.ExpendEnergy(Convert.ToInt32(Math.Max(this.RealSpeed / 2, 1)));
                if (Vector2.Distance(this.sprite.position, this.target.sprite.position) > 400)
                {
                    this.activeState = State.AnimalAssessSituation;
                    this.aiData.Reset();
                    return;
                }
            }
            else
            {
                this.activeState = State.AnimalWalkAround;
                this.aiData.Reset();
                this.aiData.SetTimeLeft(120);
                return;
            }
        }

    }

}
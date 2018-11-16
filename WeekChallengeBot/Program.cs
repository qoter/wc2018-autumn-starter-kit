using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WeekChallengeBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var centerOfGalaxy = new Vector(15, 15, 15);

            // draft
            var draftOptions = ReadInput<DraftOptions>();
            var autoDraftChoice = new DraftChoice();
            SendOutput(autoDraftChoice);

            // battle
            while (true)
            {
                var state = ReadInput<BattleState>();
                var output = new BattleOutput { Message = $"I have {state.My.Count} ships and move to center of galaxy" };

                foreach (var ship in state.My)
                {
                    output.UserCommands.Add(new UserCommand
                    {
                        Command = "MOVE",
                        Parameters = new MoveCommandParameters(ship.Id, centerOfGalaxy)
                    });

                    var gun = ship.Equipment.OfType<GunBlock>().FirstOrDefault();
                    if (gun != null)
                    {
                        output.UserCommands.Add(new UserCommand
                        {
                            Command = "ATTACK",
                            Parameters = new AttackCommandParameters(ship.Id, gun.Name, centerOfGalaxy)
                        });
                    }
                }

                SendOutput(output);
            }
        }

        #region io

        private static T ReadInput<T>()
        {
            var line = Console.ReadLine();
            return JsonConvert.DeserializeObject<T>(line);
        }

        private static void SendOutput(object output)
        {
            var outputStr = JsonConvert.SerializeObject(output);
            Console.WriteLine(outputStr);
        }

        #endregion

        #region primitives

        [JsonConverter(typeof(VectorJsonConverter))]
        public class Vector
        {
            public Vector(int x, int y, int z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public int X { get; }
            public int Y { get; }
            public int Z { get; }
        }

        public class VectorJsonConverter : JsonConverter<Vector>
        {
            public override void WriteJson(JsonWriter writer, Vector value, JsonSerializer serializer)
            {
                if (value == null)
                {
                    writer.WriteNull();
                    return;
                }

                var stringVector = $"{value.X}/{value.Y}/{value.Z}";
                writer.WriteValue(stringVector);
            }

            public override Vector ReadJson(JsonReader reader, Type objectType, Vector existingValue, bool hasExistingValue,
                                            JsonSerializer serializer)
            {
                if (reader.Value == null)
                {
                    return null;
                }
            
                if (reader.ValueType != typeof(string))
                {
                    throw new Exception(reader.Value.ToString());
                }

                var value = (string) reader.Value;
                var components = value.Split(new [] {'/'}, 3);
                if (components.Length != 3)
                {
                    throw new Exception(reader.Value.ToString());
                }

                var x = ParseComponent(components[0]);
                var y = ParseComponent(components[1]);
                var z = ParseComponent(components[2]);
            
                return new Vector(x, y, z);
            }

            private static int ParseComponent(string componentValue)
            {
                if (!int.TryParse(componentValue, out var value))
                {
                    throw new Exception(componentValue);
                }

                return value;
            }
        }

        #endregion

        #region battle commands

        public abstract class CommandParameters { }

        public class AttackCommandParameters : CommandParameters
        {
            public int Id { get; }
            public string Name { get; }
            public Vector Target { get; }

            public AttackCommandParameters(int id, string gunName, Vector target)
            {
                Id = id;
                Name = gunName;
                Target = target;
            }
        }

        public class MoveCommandParameters : CommandParameters
        {
            public int Id { get; }
            public Vector Target { get; }

            public MoveCommandParameters(int shipId, Vector target)
            {
                Id = shipId;
                Target = target;
            }
        }

        public class AccelerateCommandParameters : CommandParameters
        {
            public int Id { get; }
            public Vector Vector { get; }

            public AccelerateCommandParameters(int id, Vector vector)
            {
                Id = id;
                Vector = vector;
            }
        }

        public class UserCommand
        {
            public string Command;
            public CommandParameters Parameters;
        }

        public class BattleOutput
        {
            public List<UserCommand> UserCommands = new List<UserCommand>();
            public string Message;
        }

        #endregion

        #region draft commands 

        public class DraftChoice
        {
        }


        public class DraftOptions
        {
        }


        #endregion

        #region battle state

        public class BattleState
        {
            public List<Ship> My;
            public List<Ship> Opponent;
            public List<FireInfo> FireInfos;
        }

        public class FireInfo
        {
            public Vector Source;
            public Vector Target;
            public EffectType EffectType;
        }

        public class Ship
        {
            public int Id;
            public Vector Velocity;
            public Vector Position;
            public int Energy;
            public int? Health;
            public List<EquipmentBlock> Equipment;
        }

        #endregion

        #region equipment

        [JsonConverter(typeof(EquipmentBlockConverter))]
        public abstract class EquipmentBlock
        {
            public string Name;
            public abstract EquipmentType Type { get; }
        }

        public enum EquipmentType
        {
            Energy,
            Gun,
            Engine,
            Health
        }

        public class EquipmentBlockConverter : JsonConverter<EquipmentBlock>
        {
            public override void WriteJson(JsonWriter writer, EquipmentBlock value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override EquipmentBlock ReadJson(
                JsonReader reader,
                Type objectType,
                EquipmentBlock existingValue,
                bool hasExistingValue,
                JsonSerializer serializer)
            {
                var obj = JObject.Load(reader);
                var type = obj[nameof(EquipmentBlock.Type)].ToObject<EquipmentType>();

                EquipmentBlock equipmentBlock;
                switch (type)
                {
                    case EquipmentType.Energy:
                        equipmentBlock = new EnergyBlock();
                        break;
                    case EquipmentType.Gun:
                        equipmentBlock = new GunBlock();
                        break;
                    case EquipmentType.Engine:
                        equipmentBlock = new EngineBlock();
                        break;
                    case EquipmentType.Health:
                        equipmentBlock = new HealthBlock();
                        break;
                    default:
                        throw new NotSupportedException($"Unknown equipment {obj}");
                }
            
                serializer.Populate(obj.CreateReader(), equipmentBlock);
                return equipmentBlock;
            }
        }

        public class EnergyBlock : EquipmentBlock
        {
            public override EquipmentType Type => EquipmentType.Energy;
            public int IncrementPerTurn;
            public int MaxEnergy;
            public int StartEnergy;
        }

        public class EngineBlock : EquipmentBlock
        {
            public override EquipmentType Type => EquipmentType.Engine;
            public int MaxAccelerate;
        }

        public class GunBlock : EquipmentBlock
        {
            public override EquipmentType Type => EquipmentType.Gun;
            public int Damage;
            public int EnergyPrice;
            public int Radius;
            public EffectType EffectType;
        }

        public enum EffectType
        {
            Blaster = 0
        }

        public class HealthBlock : EquipmentBlock
        {
            public override EquipmentType Type => EquipmentType.Health;
            public int MaxHealth;
            public int StartHealth;
        }

        #endregion
    }
}

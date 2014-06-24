﻿using Havok;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Voxels;
using Sandbox.Game.Weapons;

using SEModAPI.API.SaveData;
using SEModAPI.API.SaveData.Entity;

using VRage;
using VRageMath;


namespace SEModAPI.API.Internal
{
	public class GameObjectManagerWrapper : BaseInternalWrapper
	{
		#region "Attributes"

		protected new static GameObjectManagerWrapper m_instance;

		private Thread m_mainGameThread;

		private Assembly m_assembly;

		private static Type m_objectManagerType;

		private MethodInfo m_GetObjectBuilderEntities;
		private MethodInfo m_RemoveEntity;
		private MethodInfo m_RemoveEntity2;
		private MethodInfo m_GetEntityHashSet;

		private static Vector3 m_nextEntityPosition;
		private static Vector3 m_nextEntityVelocity;

		public static string ObjectManagerClass = "5BCAC68007431E61367F5B2CF24E2D6F.CAF1EB435F77C7B77580E2E16F988BED";

		public static string ObjectManagerAction1 = "E017E9CA31926307661D7A6B465C8F96";	//() Object Manager shut down?

		public static string ObjectManagerEntityAction1 = "30E511FF32960AE853909500461285C4";	//(GameEntity) Entity-Close()
		public static string ObjectManagerEntityAction2 = "8C1807427F2EEF4DF981396C4E6A42DD";	//(GameEntity, string, string) Entity-Init()

		public static string GameEntityClass = "5BCAC68007431E61367F5B2CF24E2D6F.F6DF01EE4159339113BB9650DEEE1913";

		public static string EntityAction1 = "8CAF5306D8DF29E8140056369D0F1FC1";	//(GameEntity) OnWorldPositionChanged
		public static string EntityAction2 = "1CF14BA21D05D5F9AB6993170E4838FE";	//(GameEntity) UpdateAfterSim - Only if certain flags are set on cube blocks, not sure what yet
		public static string EntityAction3 = "183620F2B4C14EFFC9F34BFBCF35ABCC";	//(GameEntity) ??
		public static string EntityAction4 = "6C1670C128F0A838E0BE20B6EB3FB7C4";	//(GameEntity) ??
		public static string EntityAction5 = "FA752E85660B6101F92B340B994C0F29";	//(GameEntity) ??

		#endregion

		#region "Constructors and Initializers"

		protected GameObjectManagerWrapper(string basePath)
			: base(basePath)
		{
			m_instance = this;

			//string assemblyPath = Path.Combine(path, "Sandbox.Game.dll");
			m_assembly = Assembly.UnsafeLoadFrom("Sandbox.Game.dll");

			m_objectManagerType = m_assembly.GetType(ObjectManagerClass);

			m_GetObjectBuilderEntities = m_objectManagerType.GetMethod("0A1670B270D5F8417447CFCBA7BF0FA8", BindingFlags.NonPublic | BindingFlags.Static);
			m_RemoveEntity = m_objectManagerType.GetMethod("E02368B53672686387A0DE0CF91F60B7", BindingFlags.Public | BindingFlags.Static);
			m_RemoveEntity2 = m_objectManagerType.GetMethod("D46C6748B5B97161CFB9ECF64126A129", BindingFlags.Public | BindingFlags.Static);
			m_GetEntityHashSet = m_objectManagerType.GetMethod("84C54760C0F0DDDA50B0BE27B7116ED8", BindingFlags.Public | BindingFlags.Static);

			Console.WriteLine("Finished loading GameObjectManagerWrapper");
		}

		new public static GameObjectManagerWrapper GetInstance(string basePath = "")
		{
			if (m_instance == null)
			{
				m_instance = new GameObjectManagerWrapper(basePath);
			}
			return (GameObjectManagerWrapper)m_instance;
		}

		#endregion

		#region "Properties"

		public Type ObjectManagerType
		{
			get { return m_objectManagerType; }
		}

		public Thread GameThread
		{
			get { return m_mainGameThread; }
			set { m_mainGameThread = value; }
		}

		#endregion

		#region "Methods"

		public List<MyObjectBuilder_EntityBase> GetObjectBuilderEntities()
		{
			return (List<MyObjectBuilder_EntityBase>)m_GetObjectBuilderEntities.Invoke(null, new object[] { });
		}

		public HashSet<Object> GetObjectManagerHashSetData()
		{
			var rawValue = m_GetEntityHashSet.Invoke(null, new object[] { });
			HashSet<Object> convertedSet = ConvertHashSet(rawValue);

			return convertedSet;
		}

		#region APIEntityLists

		private List<T> GetAPIEntityList<T, TO>(MyObjectBuilderTypeEnum type)
			where TO : MyObjectBuilder_EntityBase
			where T : SectorObject<TO>
		{
			HashSet<Object> rawEntities = GetObjectManagerHashSetData();
			List<T> list = new List<T>();

			SetupObjectManagerEventHandlers();

			foreach (Object entity in rawEntities)
			{
				try
				{
					MyObjectBuilder_EntityBase baseEntity = (MyObjectBuilder_EntityBase)InvokeEntityMethod(entity, "GetObjectBuilder", new object[] { });

					if (baseEntity.TypeId == type)
					{
						TO objectBuilder = (TO)baseEntity;
						T apiEntity = (T)Activator.CreateInstance(typeof(T), new object[] { objectBuilder });
						apiEntity.BackingObject = entity;
						apiEntity.BackingThread = GameThread;

						SetupEntityEventHandlers(entity);

						list.Add(apiEntity);
					}
				}
				catch (Exception ex)
				{
					//TODO - Do something about the exception here
				}
			}

			return list;
		}

		public List<CubeGrid> GetCubeGrids()
		{
			return GetAPIEntityList<CubeGrid, MyObjectBuilder_CubeGrid>(MyObjectBuilderTypeEnum.CubeGrid);
		}

		public List<CharacterEntity> GetCharacters()
		{
			return GetAPIEntityList<CharacterEntity, MyObjectBuilder_Character>(MyObjectBuilderTypeEnum.Character);
		}

		public List<VoxelMap> GetVoxelMaps()
		{
			return GetAPIEntityList<VoxelMap, MyObjectBuilder_VoxelMap>(MyObjectBuilderTypeEnum.VoxelMap);
		}

		public List<FloatingObject> GetFloatingObjects()
		{
			return GetAPIEntityList<FloatingObject, MyObjectBuilder_FloatingObject>(MyObjectBuilderTypeEnum.FloatingObject);
		}

		public List<Meteor> GetMeteors()
		{
			return GetAPIEntityList<Meteor, MyObjectBuilder_Meteor>(MyObjectBuilderTypeEnum.Meteor);
		}

		#endregion

		#region Private

		private static FastResourceLock GetResourceLock()
		{
			try
			{
				FieldInfo field = m_objectManagerType.GetField("6EF7F983A8061B40A5606D75C890AF07", BindingFlags.Public | BindingFlags.Static);
				FastResourceLock resourceLock = (FastResourceLock)field.GetValue(null);

				return resourceLock;
			}
			catch (Exception ex)
			{
				return null;
			}
		}

		private static Object GetEntityPhysicsObject(Object gameEntity)
		{
			try
			{
				MethodInfo getPhysicsObjectMethod = GetEntityMethod(gameEntity, "691FA4830C80511C934826203A251981");
				Object physicsObject = getPhysicsObjectMethod.Invoke(gameEntity, new object[] { });

				return physicsObject;
			}
			catch (Exception ex)
			{
				//TODO - Find a better way to handle an exception here
				return null;
			}
		}

		private static HkRigidBody GetRigidBody(Object gameEntity)
		{
			try
			{
				Object physicsObject = GetEntityPhysicsObject(gameEntity);
				MethodInfo getRigidBodyMethod = GetEntityMethod(physicsObject, "634E5EC534E45874230868BD089055B1");
				HkRigidBody rigidBody = (HkRigidBody)getRigidBodyMethod.Invoke(physicsObject, new object[] { });

				return rigidBody;
			}
			catch (Exception ex)
			{
				//TODO - Find a better way to handle an exception here
				return null;
			}
		}

		#endregion

		#region EntityMethods

		public bool SetupObjectManagerEventHandlers()
		{
			try
			{
				FieldInfo actionField = m_objectManagerType.GetField(ObjectManagerEntityAction1, BindingFlags.NonPublic | BindingFlags.Static);
				Action<Object> newAction1 = ObjectManagerEntityEvent1;
				actionField.SetValue(null, newAction1);
				
				actionField = m_objectManagerType.GetField(ObjectManagerEntityAction2, BindingFlags.NonPublic | BindingFlags.Static);
				Action<Object, string, string> newAction2 = ObjectManagerEntityEvent2;
				actionField.SetValue(null, newAction2);

				FieldInfo actionField3 = m_objectManagerType.GetField(ObjectManagerAction1, BindingFlags.NonPublic | BindingFlags.Static);
				Action newAction3 = ObjectManagerEvent1;
				actionField3.SetValue(null, newAction3);
				
				return true;
			}
			catch (TargetInvocationException ex)
			{
				throw ex.InnerException;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public bool SetupEntityEventHandlers(Object gameEntity)
		{
			try
			{
				FieldInfo actionField = GetEntityField(gameEntity, EntityAction1);
				Action<Object> newAction = EntityOnPositionChanged;
				actionField.SetValue(gameEntity, newAction);
				
				FieldInfo actionField2 = GetEntityField(gameEntity, EntityAction2);
				Action<Object> newAction2 = EntityEvent2;
				actionField2.SetValue(gameEntity, newAction2);
				
				FieldInfo actionField3 = GetEntityField(gameEntity, EntityAction3);
				Action<Object> newAction3 = EntityEvent3;
				actionField3.SetValue(gameEntity, newAction3);
				
				FieldInfo actionField4 = GetEntityField(gameEntity, EntityAction4);
				Action<Object> newAction4 = EntityEvent4;
				actionField4.SetValue(gameEntity, newAction4);
				/*
				FieldInfo actionField5 = GetEntityField(gameEntity, EntityAction5);
				Action<Object> newAction5 = EntityEvent5;
				actionField5.SetValue(gameEntity, newAction5);
				*/

				return true;
			}
			catch (TargetInvocationException ex)
			{
				throw ex.InnerException;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		public bool UpdateEntityId(Object gameEntity, long entityId)
		{
			try
			{
				FieldInfo entityIdField = GetEntityField(gameEntity, "F7E51DBA5F2FD0CCF8BBE66E3573BEAC");
				entityIdField.SetValue(gameEntity, entityId);

				return true;
			}
			catch (Exception ex)
			{
				return false;
			}
		}

		public bool UpdateEntityPosition(Object gameEntity, Vector3 position)
		{
			try
			{
				m_nextEntityPosition = position;

				FieldInfo actionField = GetEntityField(gameEntity, EntityAction1);
				Action<Object> newAction = InternalUpdateEntityPosition;
				actionField.SetValue(gameEntity, newAction);

				return true;
			}
			catch (Exception ex)
			{
				//TODO - Find a better way to handle an exception here
				return false;
			}
		}

		public bool UpdateEntityVelocity(Object gameEntity, Vector3 velocity)
		{
			try
			{
				m_nextEntityVelocity = velocity;

				FieldInfo actionField = GetEntityField(gameEntity, EntityAction1);
				Action<Object> newAction = InternalUpdateEntityVelocity;
				actionField.SetValue(gameEntity, newAction);

				return true;
			}
			catch (Exception ex)
			{
				//TODO - Find a better way to handle an exception here
				return false;
			}
		}

		public bool RemoveEntity(Object gameEntity)
		{
			try
			{
				FieldInfo actionField = GetEntityField(gameEntity, EntityAction1);
				Action<Object> newAction = InternalRemoveEntity;
				actionField.SetValue(gameEntity, newAction);

				return true;
			}
			catch (TargetInvocationException ex)
			{
				throw ex.InnerException;
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		#region "Actions"

		public static void ObjectManagerEvent1()
		{
			try
			{
				Console.WriteLine("ObjectManagerEvent - '1'");

				FieldInfo actionField = m_objectManagerType.GetField(ObjectManagerAction1, BindingFlags.NonPublic | BindingFlags.Static);
				Action newAction = ObjectManagerEvent1;
				actionField.SetValue(null, newAction);
			}
			catch (Exception ex)
			{
				//TODO - Find the best way to handle this exception
				return;
			}
		}

		public static void ObjectManagerEntityEvent1(Object gameEntity)
		{
			try
			{
				Console.WriteLine("Entity '" + GetEntityId(gameEntity).ToString() + "': ObjectManagerEvent - Entity Closed");

				FieldInfo actionField = m_objectManagerType.GetField(ObjectManagerEntityAction1, BindingFlags.NonPublic | BindingFlags.Static);
				Action<Object> newAction = ObjectManagerEntityEvent1;
				actionField.SetValue(null, newAction);
			}
			catch (Exception ex)
			{
				//TODO - Find the best way to handle this exception
				return;
			}
		}

		public static void ObjectManagerEntityEvent2(Object gameEntity, string oldKey, string currentKey)
		{
			try
			{
				Console.WriteLine("Entity '" + GetEntityId(gameEntity).ToString() + "': ObjectManagerEvent - Entity Initialized");

				FieldInfo actionField = m_objectManagerType.GetField(ObjectManagerEntityAction2, BindingFlags.NonPublic | BindingFlags.Static);
				Action<Object, string, string> newAction = ObjectManagerEntityEvent2;
				actionField.SetValue(null, newAction);
			}
			catch (Exception ex)
			{
				//TODO - Find the best way to handle this exception
				return;
			}
		}

		public static void EntityEvent2(Object gameEntity)
		{
			try
			{
				Console.WriteLine("Entity '" + GetEntityId(gameEntity).ToString() + "': Event '2' triggered");

				FieldInfo actionField = GetEntityField(gameEntity, EntityAction2);
				Action<Object> newAction = EntityEvent2;
				actionField.SetValue(gameEntity, newAction);
			}
			catch (Exception ex)
			{
				//TODO - Find the best way to handle this exception
				return;
			}
		}

		public static void EntityEvent3(Object gameEntity)
		{
			try
			{
				Console.WriteLine("Entity '" + GetEntityId(gameEntity).ToString() + "': Event '3' triggered");

				FieldInfo actionField = GetEntityField(gameEntity, EntityAction3);
				Action<Object> newAction = EntityEvent3;
				actionField.SetValue(gameEntity, newAction);
			}
			catch (Exception ex)
			{
				//TODO - Find the best way to handle this exception
				return;
			}
		}

		public static void EntityEvent4(Object gameEntity)
		{
			try
			{
				Console.WriteLine("Entity '" + GetEntityId(gameEntity).ToString() + "': Event '4' triggered");

				FieldInfo actionField = GetEntityField(gameEntity, EntityAction4);
				Action<Object> newAction = EntityEvent4;
				actionField.SetValue(gameEntity, newAction);
			}
			catch (Exception ex)
			{
				//TODO - Find the best way to handle this exception
				return;
			}
		}

		public static void EntityEvent5(Object gameEntity)
		{
			try
			{
				Console.WriteLine("Entity '" + GetEntityId(gameEntity).ToString() + "': Event '5' triggered");

				FieldInfo actionField = GetEntityField(gameEntity, EntityAction5);
				Action<Object> newAction = EntityEvent5;
				actionField.SetValue(gameEntity, newAction);
			}
			catch (Exception ex)
			{
				//TODO - Find the best way to handle this exception
				return;
			}
		}

		public static void EntityOnPositionChanged(Object gameEntity)
		{
			try
			{
				//Console.WriteLine("Entity '" + GetEntityId(gameEntity).ToString() + "': Event 'PositionChanged' triggered");

				//Make sure the action is still tied to this event handler
				//FieldInfo actionField = GetEntityField(gameEntity, EntityAction1);
				//Action<Object> newAction = EntityOnPositionChanged;
				//actionField.SetValue(gameEntity, newAction);
			}
			catch (Exception ex)
			{
				//TODO - Find the best way to handle this exception
				return;
			}
		}

		public static void InternalUpdateEntityPosition(Object gameEntity)
		{
			try
			{
				Console.WriteLine("Entity '" + GetEntityId(gameEntity).ToString() + "': Updating position to " + m_nextEntityPosition.ToString());

				HkRigidBody havokBody = GetRigidBody(gameEntity);
				havokBody.Position = m_nextEntityPosition;
				m_nextEntityPosition = Vector3.Zero;

				//Restore the action back to the default event handler
				FieldInfo actionField = GetEntityField(gameEntity, EntityAction1);
				Action<Object> newAction = EntityOnPositionChanged;
				actionField.SetValue(gameEntity, newAction);
			}
			catch (Exception ex)
			{
				//TODO - Find the best way to handle this exception
				return;
			}
		}

		public static void InternalUpdateEntityVelocity(Object gameEntity)
		{
			try
			{
				Console.WriteLine("Entity '" + GetEntityId(gameEntity).ToString() + "': Updating velocity to " + m_nextEntityVelocity.ToString());

				HkRigidBody havokBody = GetRigidBody(gameEntity);
				havokBody.LinearVelocity = m_nextEntityVelocity;
				m_nextEntityVelocity = Vector3.Zero;

				//Restore the action back to the default event handler
				FieldInfo actionField = GetEntityField(gameEntity, EntityAction1);
				Action<Object> newAction = EntityOnPositionChanged;
				actionField.SetValue(gameEntity, newAction);
			}
			catch (Exception ex)
			{
				//TODO - Find the best way to handle this exception
				return;
			}
		}

		public static void InternalRemoveEntity(Object gameEntity)
		{
			try
			{
				Console.WriteLine("Entity '" + GetEntityId(gameEntity).ToString() + "': Calling 'Close'");

				//TODO - Find a better way to do this since just calling the Close() method causes the main game to crash
				MethodInfo method = GetEntityMethod(gameEntity, "Close");
				Object result = method.Invoke(gameEntity, new object[] {});

				//Restore the action back to the default event handler
				FieldInfo actionField = GetEntityField(gameEntity, EntityAction1);
				Action<Object> newAction = EntityOnPositionChanged;
				actionField.SetValue(gameEntity, newAction);
			}
			catch (Exception ex)
			{
				//TODO - Find the best way to handle this exception
				return;
			}
		}

		#endregion

		#endregion

		#endregion
	}
}
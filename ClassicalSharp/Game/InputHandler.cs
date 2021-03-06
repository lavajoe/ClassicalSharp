﻿using System;
using OpenTK;
using OpenTK.Input;

namespace ClassicalSharp {

	public sealed class InputHandler {
		
		Game game;
		public InputHandler( Game game ) {
			this.game = game;
			RegisterInputHandlers();
		}
		
		void RegisterInputHandlers() {
			game.Keyboard.KeyDown += KeyDownHandler;
			game.Keyboard.KeyUp += KeyUpHandler;
			game.KeyPress += KeyPressHandler;
			game.Mouse.WheelChanged += MouseWheelChanged;
			game.Mouse.Move += MouseMove;
			game.Mouse.ButtonDown += MouseButtonDown;
			game.Mouse.ButtonUp += MouseButtonUp;
		}

		bool[] buttonsDown = new bool[3];
		DateTime lastClick = DateTime.MinValue;
		public void PickBlocks( bool cooldown, bool left, bool right, bool middle ) {
			DateTime now = DateTime.UtcNow;
			double delta = ( now - lastClick ).TotalMilliseconds;
			if( cooldown && delta < 250 ) return; // 4 times per second
			lastClick = now;
			Inventory inv = game.Inventory;
			
			if( game.Network.UsingPlayerClick && !game.ScreenLockedInput ) {
				byte targetId = game.Players.GetClosetPlayer( game.LocalPlayer );
				ButtonStateChanged( MouseButton.Left, left, targetId );
				ButtonStateChanged( MouseButton.Right, right, targetId );
				ButtonStateChanged( MouseButton.Middle, middle, targetId );
			}
			
			int buttonsDown = ( left ? 1 : 0 ) + ( right ? 1 : 0 ) + ( middle ? 1 : 0 );
			if( !game.SelectedPos.Valid || buttonsDown > 1 || game.ScreenLockedInput || inv.HeldBlock == Block.Air ) return;
			
			if( middle ) {
				Vector3I pos = game.SelectedPos.BlockPos;
				byte block = game.Map.GetBlock( pos );
				if( block != 0 && (inv.CanPlace[block] || inv.CanDelete[block]) ) {
					inv.HeldBlock = (Block)block;
				}
			} else if( left ) {
				Vector3I pos = game.SelectedPos.BlockPos;
				byte block = game.Map.GetBlock( pos );
				if( block != 0 && inv.CanDelete[block] ) {
					game.ParticleManager.BreakBlockEffect( pos, block );					
					game.UpdateBlock( pos.X, pos.Y, pos.Z, 0 );
					game.Network.SendSetBlock( pos.X, pos.Y, pos.Z, false, (byte)inv.HeldBlock );
				}
			} else if( right ) {
				Vector3I pos = game.SelectedPos.TranslatedPos;
				if( !game.Map.IsValidPos( pos ) ) return;
				
				byte block = (byte)inv.HeldBlock;
				if( !game.CanPick( game.Map.GetBlock( pos ) ) && inv.CanPlace[block] 
				   && CheckIsFree( pos, block ) ) {
					game.UpdateBlock( pos.X, pos.Y, pos.Z, block );
					game.Network.SendSetBlock( pos.X, pos.Y, pos.Z, true, block );
				}
			}
		}
		
		bool CheckIsFree( Vector3I pos, byte newBlock ) {
			float height = game.BlockInfo.Height[newBlock];
			BoundingBox blockBB = new BoundingBox( pos.X, pos.Y, pos.Z, 
			                                      pos.X + 1, pos.Y + height, pos.Z + 1 );
			
			for( int id = 0; id < 255; id++ ) {
				Player player = game.Players[id];
				if( player == null ) continue;
				if( player.CollisionBounds.Intersects( blockBB ) ) return false;
			}
			
			BoundingBox localBB = game.LocalPlayer.CollisionBounds;
			if( localBB.Intersects( blockBB ) ) {
				localBB.Min.Y += 0.25f + Entity.Adjustment;
				if( localBB.Intersects( blockBB ) ) return false;
				
				// Push player up if they are jumping and trying to place a block underneath them.
				Vector3 p = game.LocalPlayer.Position;
				p.Y = pos.Y + height + Entity.Adjustment;
				LocationUpdate update = LocationUpdate.MakePos( p, false );
				game.LocalPlayer.SetLocation( update, false );
			}
			return true;
		}
		
		void ButtonStateChanged( MouseButton button, bool pressed, byte targetId ) {
			if( buttonsDown[(int)button] ) {
				game.Network.SendPlayerClick( button, false, targetId, game.SelectedPos );
				buttonsDown[(int)button] = false;
			}
			if( pressed ) {
				game.Network.SendPlayerClick( button, true, targetId, game.SelectedPos );
				buttonsDown[(int)button] = true;
			}
		}
		
		internal void ScreenChanged( Screen oldScreen, Screen newScreen ) {
			if( oldScreen != null && oldScreen.HandlesAllInput )
				lastClick = DateTime.UtcNow;
			
			if( game.Network.UsingPlayerClick ) {
				byte targetId = game.Players.GetClosetPlayer( game.LocalPlayer );
				ButtonStateChanged( MouseButton.Left, false, targetId );
				ButtonStateChanged( MouseButton.Right, false, targetId );
				ButtonStateChanged( MouseButton.Middle, false, targetId );
			}
		}
		
		
		#region Event handlers
		
		void MouseButtonUp( object sender, MouseButtonEventArgs e ) {
			if( game.activeScreen == null || !game.activeScreen.HandlesMouseUp( e.X, e.Y, e.Button ) ) {
				if( game.Network.UsingPlayerClick && e.Button <= MouseButton.Middle ) {
					byte targetId = game.Players.GetClosetPlayer( game.LocalPlayer );
					ButtonStateChanged( e.Button, false, targetId );
				}
			}
		}

		void MouseButtonDown( object sender, MouseButtonEventArgs e ) {
			if( game.activeScreen == null || !game.activeScreen.HandlesMouseClick( e.X, e.Y, e.Button ) ) {
				bool left = e.Button == MouseButton.Left;
				bool right = e.Button == MouseButton.Right;
				bool middle = e.Button == MouseButton.Middle;
				PickBlocks( false, left, right, middle );
			} else {
				lastClick = DateTime.UtcNow;
			}
		}

		void MouseMove( object sender, MouseMoveEventArgs e ) {
			if( game.activeScreen == null || !game.activeScreen.HandlesMouseMove( e.X, e.Y ) ) {
			}
		}

		void MouseWheelChanged( object sender, MouseWheelEventArgs e ) {
			if( game.activeScreen == null || !game.activeScreen.HandlesMouseScroll( e.Delta ) ) {
				Inventory inv = game.Inventory;
				if( game.Camera.MouseZoom( e ) || !inv.CanChangeHeldBlock ) return;
				
				int diff = -e.Delta % inv.Hotbar.Length;
				int newIndex = inv.HeldBlockIndex + diff;
				if( newIndex < 0 ) newIndex += inv.Hotbar.Length;
				if( newIndex >= inv.Hotbar.Length ) newIndex -= inv.Hotbar.Length;
				inv.HeldBlockIndex = newIndex;
			}
		}

		void KeyPressHandler( object sender, KeyPressEventArgs e ) {
			char key = e.KeyChar;
			if( game.activeScreen == null || !game.activeScreen.HandlesKeyPress( key ) ) {
			}
		}
		
		void KeyUpHandler( object sender, KeyboardKeyEventArgs e ) {
			Key key = e.Key;
			if( game.activeScreen == null || !game.activeScreen.HandlesKeyUp( key ) ) {
			}
		}

		static int[] viewDistances = { 16, 32, 64, 128, 256, 512 };
		void KeyDownHandler( object sender, KeyboardKeyEventArgs e ) {
			Key key = e.Key;
			if( key == Key.F4 && (game.IsKeyDown( Key.AltLeft ) || game.IsKeyDown( Key.AltRight )) ) {
				game.Exit();
			} else if( key == game.Keys[KeyMapping.Screenshot] ) {
				game.screenshotRequested = true;
			} else if( game.activeScreen == null || !game.activeScreen.HandlesKeyDown( key ) ) {
				if( !HandleBuiltinKey( key ) ) {
					game.LocalPlayer.HandleKeyDown( key );
				}
			}
		}
		
		bool HandleBuiltinKey( Key key ) {
			if( key == game.Keys[KeyMapping.HideGui] ) {
				game.HideGui = !game.HideGui;
			} else if( key == game.Keys[KeyMapping.Fullscreen] ) {
				WindowState state = game.WindowState;
				if( state != WindowState.Minimized ) {
					game.WindowState = state == WindowState.Fullscreen ?
						WindowState.Normal : WindowState.Fullscreen;
				}
			} else if( key == game.Keys[KeyMapping.ThirdPersonCamera] ) {
				bool useThirdPerson = game.Camera is FirstPersonCamera;
				game.SetCamera( useThirdPerson );
			} else if( key == game.Keys[KeyMapping.ViewDistance] ) {
				for( int i = 0; i < viewDistances.Length; i++ ) {
					int newDist = viewDistances[i];
					if( newDist > game.ViewDistance ) {
						game.SetViewDistance( newDist );
						return true;
					}
				}
				game.SetViewDistance( viewDistances[0] );
			} else if( key == game.Keys[KeyMapping.PauseOrExit] && !game.Map.IsNotLoaded ) {
				game.SetNewScreen( new PauseScreen( game ) );
			} else if( key == game.Keys[KeyMapping.OpenInventory] ) {
				game.SetNewScreen( new BlockSelectScreen( game ) );
			} else {
				return false;
			}
			return true;
		}
		
		#endregion
	}
}
﻿using System;
using System.Drawing;
using ClassicalSharp.GraphicsAPI;
using OpenTK.Input;

namespace ClassicalSharp {
	
	public abstract class Widget : IDisposable {
		
		protected internal Game game;
		protected IGraphicsApi graphicsApi;
		
		public Widget( Game game ) {
			this.game = game;
			graphicsApi = game.Graphics;
			HorizontalDocking = Docking.LeftOrTop;
			VerticalDocking = Docking.LeftOrTop;
		}
		
		public int X { get; set; }
		
		public int Y { get; set; }
		
		public int Width { get; set; }
		
		public int Height { get; set; }
		
		public Docking HorizontalDocking { get; set; }
		
		public Docking VerticalDocking { get; set; }
		
		public Size Size {
			get { return new Size( Width, Height ); }
		}
		
		public Point TopLeft {
			get { return new Point( X, Y ); }
		}
		
		public Point BottomRight {
			get { return new Point( X + Width, Y + Height ); }
		}
		
		public Rectangle Bounds {
			get { return new Rectangle( X, Y, Width, Height ); }
		}
		
		public bool ContainsPoint( int x, int y ) {
			return Bounds.Contains( x, y );
		}
		
		public bool ContainsPoint( Point point ) {
			return Bounds.Contains( point );
		}

		public bool ContainsRectangle( Rectangle rect ) {
			return Bounds.Contains( rect );
		}

		public bool IntersectsRectangle( Rectangle rect ) {
			return Bounds.IntersectsWith( rect );
		}
		
		public virtual bool HandlesKeyDown( Key key ) {
			return false;
		}
		
		public virtual bool HandlesKeyPress( char key ) {
			return false;
		}
		
		public virtual bool HandlesKeyUp( Key key ) {
			return false;
		}
		
		public virtual bool HandleMouseClick( int mouseX, int mouseY ) {
			return false;
		}
		
		public virtual bool HandleMouseHover( int mouseX, int mouseY ) {
			return false;
		}
		
		public virtual bool HandleMouseLeave( int mouseX, int mouseY ) {
			return false;
		}
		
		public virtual bool HandleMouseDeClick( int mouseX, int mouseY ) {
			return false;
		}
		
		public abstract void Init();
		
		public abstract void Render( double delta );
		
		public abstract void Dispose();
		
		public virtual void MoveTo( int newX, int newY ) {
			X = newX;
			Y = newY;
		}
		
		public virtual void OnResize( int oldWidth, int oldHeight, int width, int height ) {
			int deltaX = CalcDelta( width, oldWidth, HorizontalDocking );
			int deltaY = CalcDelta( height, oldHeight, VerticalDocking );
			MoveTo( X + deltaX, Y + deltaY );
		}
		
		protected static int CalcDelta( int newVal, int oldVal, Docking mode ) {
			return CalcOffset( newVal, oldVal, 0, mode );
		}
		
		protected static int CalcOffset( int axisSize, int elemSize, int offset, Docking mode ) {
			if( mode == Docking.LeftOrTop ) return offset;
			if( mode == Docking.BottomOrRight) return axisSize - elemSize - offset;
			return ( axisSize - elemSize ) / 2 + offset;
		}
	}
	
	public enum Docking {
		LeftOrTop = 0,
		Centre = 1,
		BottomOrRight = 2,
	}
}
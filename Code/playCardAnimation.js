/*
Play Card Animation - Allowed our game to include adapting animations when we realized the artists were not going to be able to 
bake animations in that would suit all cards on all levels on all device sizes.
*/

playCardAnimation: function ( itemViewList, animationName, animationDuration, callback, callbackTarget )
		{
			// CHANGE THIS TO SPEEDUP/SLOWDOWN THE TIME BETWEEN THE FIRST CARD STARTING ITS ANIMATION AND THE LAST CARD STARTING ITS ANIMATION
			var sweepDuration = .2
			var screenWidth = cc.winSize.width;

			this.animationItemsViewList = itemViewList;
			//tell each card to play their animation after a delay (delay dependent on their x position relative to screenWidth)
			for ( var i = 0; i < this.animationItemsViewList.length; i++ )
			{
				var currentItemView = this.animationItemsViewList[ i ];
				var delayTime = ( ( currentItemView.node.x + currentItemView.node.parent.width / 2 ) / currentItemView.node.parent.width ) * sweepDuration;//screenWidth / 2);
				var delay = cc.delayTime( delayTime );
				var func = cc.callFunc( currentItemView.playAnimationByName, currentItemView, animationName );
				var seq = cc.sequence( delay, func );
				currentItemView.node.runAction( seq );
			}

			if ( this.animationItemsViewList.length > 0 )
			{
				//tell the game to delay for the time it takes to sweep plus the time it takes to do the animation (this will play after the last card has played its animation)
				var gameDelay = cc.delayTime( sweepDuration + animationDuration )
				var continueGameFunc = cc.callFunc( callback, callbackTarget );
				var gameSeq = cc.sequence( gameDelay, continueGameFunc );
				this.node.runAction( gameSeq );
			}
			else 
			{
				callback();
			}


		}

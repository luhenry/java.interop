﻿using System;

using Java.Interop;

namespace Java.InteropTests {

	[JniTypeInfo (CallVirtualFromConstructorBase.JniTypeName)]
	public class CallVirtualFromConstructorBase : JavaObject {

		internal    const   string          JniTypeName = "com/xamarin/interop/CallVirtualFromConstructorBase";
		readonly    static  JniPeerMembers  _members    = new JniPeerMembers (JniTypeName, typeof (CallVirtualFromConstructorBase));

		public override JniPeerMembers JniPeerMembers {
			get {return _members;}
		}

		public CallVirtualFromConstructorBase (int value)
			: base (null, 0)
		{
			using (SetSafeHandle (
						JniPeerMembers.InstanceMethods.StartCreateInstance ("(I)V", GetType (), value),
						JniHandleOwnership.Transfer)) {
				JniPeerMembers.InstanceMethods.FinishCreateInstance ("(I)V", this, value);
			}
		}

		public virtual void CalledFromConstructor (int value)
		{
			_members.InstanceMethods.CallVoidMethod ("calledFromConstructor\u0000(I)V", this, value);
		}
	}
}

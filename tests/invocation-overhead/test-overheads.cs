using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Java.Interop;

using SafeEnv       = Java.Interop.SafeHandles.JniEnvironment;
using JIIntPtrEnv   = Java.Interop.JIIntPtrs.JniEnvironment;
using PinvokeEnv    = Java.Interop.JIPinvokes.JniEnvironment;
using XAIntPtrEnv   = Java.Interop.XAIntPtrs.JniEnvironment;

namespace Java.Interop {
	public enum JniObjectReferenceType {
		Invalid     = 0,
		Local       = 1,
		Global      = 2,
		WeakGlobal  = 3,
	}

	public enum JniReleaseArrayElementsMode {
		Default,
		Commit,
		Abort,
	}

	public struct JniObjectReference
	{
		public  JniReferenceSafeHandle      SafeHandle  {get; private set;}
		public  IntPtr                      Handle  {get; private set;}
		public  JniObjectReferenceType      Type    {get; private set;}
		public  bool                        IsValid {
			get {
				return (SafeHandle != null && !SafeHandle.IsInvalid) ||
					Handle != IntPtr.Zero;
			}
		}

		public JniObjectReference (JniReferenceSafeHandle handle, JniObjectReferenceType type = JniObjectReferenceType.Invalid)
		{
			SafeHandle = handle;
			Handle  = IntPtr.Zero;
			Type    = type;
		}

		public JniObjectReference (IntPtr handle, JniObjectReferenceType type = JniObjectReferenceType.Invalid)
		{
			SafeHandle = null;
			Handle  = handle;
			Type    = type;
		}
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct JniArgumentValue {
#pragma warning disable 0414
		[FieldOffset(0)] bool z;
		[FieldOffset(0)] sbyte b;
		[FieldOffset(0)] char c;
		[FieldOffset(0)] short s;
		[FieldOffset(0)] int i;
		[FieldOffset(0)] long j;
		[FieldOffset(0)] float f;
		[FieldOffset(0)] double d;
		[FieldOffset(0)] IntPtr l;
#pragma warning restore 0414

		public JniArgumentValue (bool value)
		{
			this = new JniArgumentValue ();
			z = value;
		}

		public JniArgumentValue (sbyte value)
		{
			this = new JniArgumentValue ();
			b = value;
		}

		public JniArgumentValue (char value)
		{
			this = new JniArgumentValue ();
			c = value;
		}

		public JniArgumentValue (short value)
		{
			this = new JniArgumentValue ();
			s = value;
		}

		public JniArgumentValue (int value)
		{
			this = new JniArgumentValue ();
			i = value;
		}

		public JniArgumentValue (long value)
		{
			this = new JniArgumentValue ();
			j = value;
		}

		public JniArgumentValue (float value)
		{
			this = new JniArgumentValue ();
			f = value;
		}

		public JniArgumentValue (double value)
		{
			this = new JniArgumentValue ();
			d = value;
		}
		public JniArgumentValue (IntPtr value)
		{
			this = new JniArgumentValue ();
			l = value;
		}
		public JniArgumentValue (JniObjectReference value)
		{
			this = new JniArgumentValue ();
			var sh = value.SafeHandle;
			if (sh != null)
				l = value.SafeHandle.DangerousGetHandle ();
			else
				l = value.Handle;
		}

		public JniArgumentValue (JniReferenceSafeHandle value)
		{
			this = new JniArgumentValue ();
			l = value == null ? IntPtr.Zero : value.DangerousGetHandle ();
		}

		public override string ToString ()
		{
			return string.Format ("Java.Interop.JniArgumentValue(z={0},b={1},c={2},s={3},i={4},f={5},d={6},l=0x{7})",
					z, b, c, s, i, f, d, l.ToString ("x"));
		}
	}
	public sealed class JniFieldInfo
	{
		public IntPtr ID;
		public bool   IsStatic;
		public bool IsValid {get {return ID != IntPtr.Zero;}}

		public JniFieldInfo (IntPtr id, bool isStatic)
		{
			ID = id;
			IsStatic = isStatic;
		}
		public JniFieldInfo (string name, string signature, IntPtr id, bool isStatic)
		{
			ID = id;
			IsStatic = isStatic;
		}

		public override string ToString ()
		{
			return string.Format ("{0}(0x{1})", GetType ().FullName, ID.ToString ("x"));
		}
	}

	public class JniMethodInfo
	{
		public IntPtr ID;
		public bool   IsStatic;
		public bool IsValid {get {return ID != IntPtr.Zero;}}
		public JniMethodInfo (IntPtr id, bool isStatic)
		{
			ID = id;
			IsStatic = isStatic;
		}
		public JniMethodInfo (string name, string signature, IntPtr id, bool isStatic)
		{
			ID = id;
			IsStatic = isStatic;
		}
		public override string ToString ()
		{
			return string.Format ("{0}(0x{1})", GetType ().FullName, ID.ToString ("x"));
		}
	}
	public struct JniNativeMethodRegistration {

		public  string      Name;
		public  string      Signature;
		public  Delegate    Marshaler;

		public JniNativeMethodRegistration (string name, string signature, Delegate marshaler)
		{
			Name        = name;
			Signature   = signature;
			Marshaler   = marshaler;
		}
	}

	public abstract class JniReferenceSafeHandle : SafeHandle
	{
		protected JniReferenceSafeHandle ()
			: this (ownsHandle:true)
		{
		}

		internal JniReferenceSafeHandle (bool ownsHandle)
			: base (IntPtr.Zero, ownsHandle)
		{
		}

		public override bool IsInvalid {
			get {return base.handle == IntPtr.Zero;}
		}

		public JniObjectReferenceType ReferenceType {
			get {
				if (IsInvalid)
					throw new ObjectDisposedException (GetType ().FullName);
				return SafeHandles.JniEnvironment.References.GetObjectRefType (new JniObjectReference (this));
			}
		}

		internal IntPtr _GetAndClearHandle ()
		{
			var h   = handle;
			handle  = IntPtr.Zero;
			return h;
		}

		public override string ToString ()
		{
			return string.Format ("{0}(0x{1})", GetType ().FullName, handle.ToString ("x"));
		}
	}
	public class JniLocalReference : JniReferenceSafeHandle {

		internal JniLocalReference ()
		{
		}

		protected override bool ReleaseHandle ()
		{
			SafeHandles.JniEnvironment.References.DeleteLocalRef (handle);
			return true;
		}
	}
	public class JniWeakGlobalReference : JniReferenceSafeHandle {
		protected override bool ReleaseHandle ()
		{
			Console.WriteLine ("# {0}.ReleaseHandle()", GetType ().FullName);
			SafeHandles.JniEnvironment.References.DeleteWeakGlobalRef (handle);
			return true;
		}
	}
	public class JniGlobalReference : JniReferenceSafeHandle {
		protected override bool ReleaseHandle ()
		{
			Console.WriteLine ("# {0}.ReleaseHandle()", GetType ().FullName);
			SafeHandles.JniEnvironment.References.DeleteGlobalRef (handle);
			return true;
		}
	}
	public sealed class JniEnvironmentSafeHandle : SafeHandle
	{
		JniEnvironmentSafeHandle ()
			: base (IntPtr.Zero, ownsHandle:false)
		{
		}

		public JniEnvironmentSafeHandle (IntPtr handle)
			: this ()
		{
			SetHandle (handle);
		}

		public override bool IsInvalid {
			get {return handle == IntPtr.Zero;}
		}

		protected override bool ReleaseHandle ()
		{
			Console.WriteLine ("# {0}.ReleaseHandle()", GetType ().FullName);
			return false;
		}

		internal unsafe SafeHandles.JniEnvironmentInvoker CreateInvoker ()
		{
			IntPtr p = Marshal.ReadIntPtr (handle);
			return new SafeHandles.JniEnvironmentInvoker ((JniNativeInterfaceStruct*) p);
		}

		public override string ToString ()
		{
			return string.Format ("{0}(0x{1})", GetType ().FullName, handle.ToString ("x"));
		}
	}
	public sealed class JavaVMSafeHandle : SafeHandle {

		JavaVMSafeHandle ()
			: base (IntPtr.Zero, ownsHandle:false)
		{
		}

		public JavaVMSafeHandle (IntPtr handle)
			: this ()
		{
			SetHandle (handle);
		}

		public override bool IsInvalid {
			get {return handle == IntPtr.Zero;}
		}

		internal IntPtr Handle {
			get {return base.handle;}
		}

		protected override bool ReleaseHandle ()
		{
			Console.WriteLine ("# {0}.ReleaseHandle()", GetType ().FullName);
			return false;
		}

		public override string ToString ()
		{
			return string.Format ("{0}(0x{1})", GetType ().FullName, handle.ToString ("x"));
		}
	}
	struct JavaVMInitArgs {
		public  JniVersion                      version;    /*				 use JNI_VERSION_1_2 or later */

		public  int                             nOptions;
		public  IntPtr /* JavaVMOption[] */     options;
		public  byte                            ignoreUnrecognized;
	}

	struct JavaVMOption {
		public  IntPtr /* const char* */    optionString;
		public  IntPtr /* void * */         extraInfo;
	}
	public enum JniVersion {
		// v1_1    = 0x00010001,
		v1_2    = 0x00010002,
		v1_4    = 0x00010004,
		v1_6	= 0x00010006,
	}
}

namespace Java.Interop.SafeHandles {
	class JniEnvironmentInfo {
		public IntPtr EnvironmentPointer;
		public JniEnvironmentInvoker Invoker;
	}
	public static partial class JniEnvironment {
		internal static JniEnvironmentInfo CurrentInfo;

		internal static void LogCreateLocalRef (JniLocalReference value)
		{
		}
		public static Exception GetExceptionForLastThrowable ()
		{
			var v = SafeEnv.Exceptions.ExceptionOccurred ();
			if (v.SafeHandle == null || v.SafeHandle.IsInvalid || v.SafeHandle.IsClosed)
				return null;
			Console.WriteLine ("exception?!");
			SafeEnv.Exceptions.ExceptionDescribe();
			SafeEnv.Exceptions.ExceptionClear ();
			LogCreateLocalRef ((JniLocalReference) v.SafeHandle);
			v.SafeHandle.Dispose ();
			return new Exception ("yada yada yada");
		}
	}
}

namespace Java.Interop.JIIntPtrs {
	class JniEnvironmentInfo {
		public IntPtr EnvironmentPointer;
		public JniEnvironmentInvoker Invoker;
	}
	public static partial class JniEnvironment {
		internal static JniEnvironmentInfo CurrentInfo;

		internal static void LogCreateLocalRef (IntPtr value)
		{
		}
		public static Exception GetExceptionForLastThrowable ()
		{
			var v = JIIntPtrEnv.Exceptions.ExceptionOccurred ();
			var h = v.Handle;
			if (h == IntPtr.Zero)
				return null;
			JIIntPtrEnv.Exceptions.ExceptionClear ();
			LogCreateLocalRef (h);
			JIIntPtrEnv.References.DeleteLocalRef (h);
			return new Exception ("yada yada yada");
		}
	}
}

namespace Java.Interop.JIPinvokes {
	public static partial class JniEnvironment {
		public static IntPtr EnvironmentPointer;

		internal static void LogCreateLocalRef (IntPtr value)
		{
		}
		public static Exception GetExceptionForLastThrowable (IntPtr h)
		{
			if (h == IntPtr.Zero)
				return null;
			PinvokeEnv.Exceptions.ExceptionClear ();
			LogCreateLocalRef (h);
			var r = new JniObjectReference (h, JniObjectReferenceType.Local);
			PinvokeEnv.References.DeleteLocalRef (r.Handle);
			return new Exception ("yada yada yada");
		}
		public static Exception GetExceptionForLastThrowable ()
		{
			var v = JIIntPtrEnv.Exceptions.ExceptionOccurred ();
			var h = v.Handle;
			if (h == IntPtr.Zero)
				return null;
			SafeEnv.Exceptions.ExceptionClear ();
			LogCreateLocalRef (h);
			JIIntPtrEnv.References.DeleteLocalRef (h);
			return new Exception ("yada yada yada");
		}
	}
}
namespace Java.Interop.XAIntPtrs {
	class JniEnvironmentInfo {
		public IntPtr EnvironmentPointer;
		public JniEnvironmentInvoker Invoker;
	}
	public partial class JniEnvironment {
		internal static JniEnvironmentInfo CurrentInfo;

		internal static void LogCreateLocalRef (IntPtr value)
		{
		}
		public static Exception GetExceptionForLastThrowable ()
		{
			var h = XAIntPtrEnv.Exceptions.ExceptionOccurred ();
			if (h == IntPtr.Zero)
				return null;
			XAIntPtrEnv.Exceptions.ExceptionClear ();
			LogCreateLocalRef (h);
			XAIntPtrEnv.References.DeleteLocalRef (h);
			return new Exception ("yada yada yada");
		}
	}
}


class App {
	const string LibraryName = "jvm.dll";

	[DllImport (LibraryName)]
	static extern int JNI_CreateJavaVM (out IntPtr javavm, out IntPtr jnienv, ref JavaVMInitArgs args);

	public static void Main ()
	{
		IntPtr _jvm, _env;
		CreateJavaVM (out _jvm, out _env);

		SafeTiming (_env);
		JIIntPtrTiming (_env);
		JIPinvokeTiming (_env);
		XAIntPtrTiming (_env);
	}

	static void CreateJavaVM (out IntPtr jvm, out IntPtr jnienv)
	{
		var args = new JavaVMInitArgs () {
			version             = JniVersion.v1_6,
			nOptions            = 0,
			ignoreUnrecognized  = (byte) 1,
		};
		int r = JNI_CreateJavaVM (out jvm, out jnienv, ref args);
		if (r != 0)
			throw new InvalidOperationException ("JNI_CreateJavaVM returned: " + r);
	}

	const int C = 10000000;

	static unsafe void SafeTiming (IntPtr _env)
	{
		SafeEnv.CurrentInfo = new Java.Interop.SafeHandles.JniEnvironmentInfo {
			EnvironmentPointer  = _env,
			Invoker             = new Java.Interop.SafeHandles.JniEnvironmentInvoker ((JniNativeInterfaceStruct*) Marshal.ReadIntPtr (_env)),
		};
		var Arrays_class = SafeEnv.Types._FindClass ("java/util/Arrays");
		var Arrays_binarySearch = SafeEnv.StaticMethods.GetStaticMethodID (Arrays_class, "binarySearch", "([II)I");
		var intArray = SafeEnv.Arrays.NewIntArray (3);
		fixed (int* p = new int[]{1,2,3})
			SafeEnv.Arrays.SetIntArrayRegion (intArray, 0, 3, p);

		var t = Stopwatch.StartNew ();
		var args = stackalloc JniArgumentValue [2];
		args [0] = new JniArgumentValue (intArray);
		args [1] = new JniArgumentValue (2);
		for (int i = 0; i < C; ++i) {
			int r = SafeEnv.StaticMethods.CallStaticIntMethod (Arrays_class, Arrays_binarySearch, args);
		}
		t.Stop ();
		Console.WriteLine ("# {0} timing: {1}", nameof (SafeTiming), t.Elapsed);
		Console.WriteLine ("#\tAverage Invocation: {0}ms", t.Elapsed.TotalMilliseconds / C);
		GC.KeepAlive (intArray);
		GC.KeepAlive (Arrays_class);
		GC.KeepAlive (Arrays_binarySearch);
	}

	static unsafe void JIIntPtrTiming (IntPtr _env)
	{
		JIIntPtrEnv.CurrentInfo = new Java.Interop.JIIntPtrs.JniEnvironmentInfo {
			EnvironmentPointer  = _env,
			Invoker             = new Java.Interop.JIIntPtrs.JniEnvironmentInvoker ((JniNativeInterfaceStruct*) Marshal.ReadIntPtr (_env)),
		};
		var Arrays_class = JIIntPtrEnv.Types._FindClass ("java/util/Arrays");
		var Arrays_binarySearch = JIIntPtrEnv.StaticMethods.GetStaticMethodID (Arrays_class, "binarySearch", "([II)I");
		var intArray = JIIntPtrEnv.Arrays.NewIntArray (3);
		fixed (int* p = new int[]{1,2,3})
			JIIntPtrEnv.Arrays.SetIntArrayRegion (intArray, 0, 3, p);

		var t = Stopwatch.StartNew ();
		var args = stackalloc JniArgumentValue [2];
		args [0] = new JniArgumentValue (intArray);
		args [1] = new JniArgumentValue (2);
		for (int i = 0; i < C; ++i) {
			int r = JIIntPtrEnv.StaticMethods.CallStaticIntMethod (Arrays_class, Arrays_binarySearch, args);
		}
		t.Stop ();
		Console.WriteLine ("# {0} timing: {1}", nameof (JIIntPtrTiming), t.Elapsed);
		Console.WriteLine ("#\tAverage Invocation: {0}ms", t.Elapsed.TotalMilliseconds / C);
	}

	static unsafe void JIPinvokeTiming (IntPtr _env)
	{
		PinvokeEnv.EnvironmentPointer = _env;
		var Arrays_class = PinvokeEnv.Types._FindClass ("java/util/Arrays");
		var Arrays_binarySearch = PinvokeEnv.StaticMethods.GetStaticMethodID (Arrays_class, "binarySearch", "([II)I");
		var intArray = PinvokeEnv.Arrays.NewIntArray (3);
		fixed (int* p = new int[]{1,2,3})
			PinvokeEnv.Arrays.SetIntArrayRegion (intArray, 0, 3, p);

		var t = Stopwatch.StartNew ();
		var args = stackalloc JniArgumentValue [2];
		args [0] = new JniArgumentValue (intArray);
		args [1] = new JniArgumentValue (2);
		for (int i = 0; i < C; ++i) {
			int r = PinvokeEnv.StaticMethods.CallStaticIntMethod (Arrays_class, Arrays_binarySearch, args);
		}
		t.Stop ();
		Console.WriteLine ("# {0} timing: {1}", nameof (JIPinvokeTiming), t.Elapsed);
		Console.WriteLine ("#\tAverage Invocation: {0}ms", t.Elapsed.TotalMilliseconds / C);
	}

	static unsafe void XAIntPtrTiming (IntPtr _env)
	{
		XAIntPtrEnv.CurrentInfo = new Java.Interop.XAIntPtrs.JniEnvironmentInfo {
			EnvironmentPointer  = _env,
			Invoker             = new Java.Interop.XAIntPtrs.JniEnvironmentInvoker ((JniNativeInterfaceStruct*) Marshal.ReadIntPtr (_env)),
		};
		var Arrays_class = XAIntPtrEnv.Types._FindClass ("java/util/Arrays");
		var Arrays_binarySearch = XAIntPtrEnv.StaticMethods.GetStaticMethodID (Arrays_class, "binarySearch", "([II)I");
		var intArray = XAIntPtrEnv.Arrays.NewIntArray (3);
		fixed (int* p = new int[]{1,2,3})
			XAIntPtrEnv.Arrays.SetIntArrayRegion (intArray, 0, 3, p);

		var t = Stopwatch.StartNew ();
		var args = stackalloc JniArgumentValue [2];
		args [0] = new JniArgumentValue (intArray);
		args [1] = new JniArgumentValue (2);
		for (int i = 0; i < C; ++i) {
			int r = XAIntPtrEnv.StaticMethods.CallStaticIntMethod (Arrays_class, Arrays_binarySearch, args);
		}
		t.Stop ();
		Console.WriteLine ("# {0} timing: {1}", nameof (XAIntPtrTiming), t.Elapsed);
		Console.WriteLine ("#\tAverage Invocation: {0}ms", t.Elapsed.TotalMilliseconds / C);
	}
}

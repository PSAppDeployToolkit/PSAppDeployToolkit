using System;
using System.Collections;
using System.Runtime.InteropServices;
using DirectShowLib;

// Code of MediaPortal (www.team-mediaportal.com)

namespace iNKORE.UI.WPF.DirectX.DirectShow.MediaPlayers
{
    public static class DirectShowUtil
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DirectShowUtil));

        public static IBaseFilter AddFilterToGraph(IGraphBuilder graphBuilder, string strFilterName, Guid clsid)
        {
            if (string.IsNullOrEmpty(strFilterName))
                return null;

            try
            {
                IBaseFilter NewFilter = null;
                foreach (Filter filter in Filters.LegacyFilters)
                {
                    if (String.Compare(filter.Name, strFilterName, true) == 0 && (clsid == Guid.Empty || filter.CLSID == clsid))
                    {
                        NewFilter = (IBaseFilter)Marshal.BindToMoniker(filter.MonikerString);

                        int hr = graphBuilder.AddFilter(NewFilter, strFilterName);
                        if (hr < 0)
                        {
                            log.Error("Unable to add filter: {0} to graph", strFilterName);
                            NewFilter = null;
                        }
                        else
                        {
                            log.Debug("Added filter: {0} to graph", strFilterName);
                        }
                        break;
                    }
                }
                if (NewFilter == null)
                {
                    log.Error("Failed filter: {0} not found", strFilterName);
                }
                return NewFilter;
            }
            catch (Exception ex)
            {
                log.Error(ex, "Error adding filter: {0} to graph", strFilterName);
                return null;
            }
        }



        public static bool DisconnectAllPins(IGraphBuilder graphBuilder, IBaseFilter filter)
        {
            IEnumPins pinEnum;
            int hr = filter.EnumPins(out pinEnum);
            if (hr != 0 || pinEnum == null)
            {
                return false;
            }
            FilterInfo info;
            filter.QueryFilterInfo(out info);

            Marshal.ReleaseComObject(info.pGraph);
            bool allDisconnected = true;
            for (; ; )
            {
                IPin[] pins = new IPin[1];
                IntPtr fetched = IntPtr.Zero;
                hr = pinEnum.Next(1, pins, fetched);
                if (hr != 0 || fetched == IntPtr.Zero)
                {
                    break;
                }
                PinInfo pinInfo;
                pins[0].QueryPinInfo(out pinInfo);
                DsUtils.FreePinInfo(pinInfo);
                if (pinInfo.dir == PinDirection.Output)
                {
                    if (!DisconnectPin(graphBuilder, pins[0]))
                    {
                        allDisconnected = false;
                    }
                }
                Marshal.ReleaseComObject(pins[0]);
            }
            Marshal.ReleaseComObject(pinEnum);
            return allDisconnected;
        }

        public static bool DisconnectPin(IGraphBuilder graphBuilder, IPin pin)
        {
            IPin other;
            int hr = pin.ConnectedTo(out other);
            bool allDisconnected = true;
            PinInfo info;
            pin.QueryPinInfo(out info);
            DsUtils.FreePinInfo(info);

            if (hr == 0 && other != null)
            {
                other.QueryPinInfo(out info);
                if (!DisconnectAllPins(graphBuilder, info.filter))
                {
                    allDisconnected = false;
                }
                hr = pin.Disconnect();
                if (hr != 0)
                {
                    allDisconnected = false;

                }
                hr = other.Disconnect();
                if (hr != 0)
                {
                    allDisconnected = false;

                }
                DsUtils.FreePinInfo(info);
                Marshal.ReleaseComObject(other);
            }
            else
            {

            }
            return allDisconnected;
        }

        public static void RemoveFilters(IGraphBuilder graphBuilder)
        {
            RemoveFilters(graphBuilder, string.Empty);
        }
        public static void RemoveFilters(IGraphBuilder graphBuilder, string filterName)
        {
            if (graphBuilder == null)
            {
                return;
            }

            int hr = 0;
            IEnumFilters enumFilters = null;
            ArrayList filtersArray = new ArrayList();

            try
            {
                hr = graphBuilder.EnumFilters(out enumFilters);
                DsError.ThrowExceptionForHR(hr);

                IBaseFilter[] filters = new IBaseFilter[1];
                IntPtr fetched = IntPtr.Zero;

                while (enumFilters.Next(filters.Length, filters, fetched) == 0)
                {
                    filtersArray.Add(filters[0]);
                }

                foreach (IBaseFilter filter in filtersArray)
                {
                    FilterInfo info;
                    filter.QueryFilterInfo(out info);
                    Marshal.ReleaseComObject(info.pGraph);

                    try
                    {
                        if (!String.IsNullOrEmpty(filterName))
                        {
                            if (String.Equals(info.achName, filterName))
                            {
                                DisconnectAllPins(graphBuilder, filter);
                                hr = graphBuilder.RemoveFilter(filter);
                                DsError.ThrowExceptionForHR(hr);
                                Marshal.ReleaseComObject(filter);
                                log.Debug("Remove filter from graph: {0}", info.achName);
                            }
                        }
                        else
                        {
                            DisconnectAllPins(graphBuilder, filter);
                            hr = graphBuilder.RemoveFilter(filter);
                            DsError.ThrowExceptionForHR(hr);
                            int i = Marshal.ReleaseComObject(filter);
                            log.Debug(string.Format("Remove filter from graph: {0} {1}", info.achName, i));
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex, "Remove of filter failed with code (HR): {0}, explanation: {1}", info.achName, hr.ToString());
                    }
                }
            }
            catch (Exception)
            {
                return;
            }
            finally
            {
                if (enumFilters != null)
                {
                    Marshal.ReleaseComObject(enumFilters);
                }
            }
        }


    }
}
